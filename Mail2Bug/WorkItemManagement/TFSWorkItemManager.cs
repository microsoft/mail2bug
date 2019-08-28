using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using log4net;
using Mail2Bug.Email;
using Mail2Bug.ExceptionClasses;
using Mail2Bug.Helpers;
using Mail2Bug.MessageProcessingStrategies;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Mail2Bug.WorkItemManagement
{
    public class TFSWorkItemManager : IWorkItemManager, IDisposable
    {
        public SortedList<string, int> WorkItemsCache { get; private set; }

        public TFSWorkItemManager(Config.InstanceConfig config)
        {
            ValidateConfig(config);

            _config = config;

            // Init TFS service objects
            _tfsServer = ConnectToTfsCollection();
            Logger.InfoFormat("Connected to TFS. Getting TFS WorkItemStore");

            _tfsStore = _config.TfsServerConfig.BypassRules
                ? new WorkItemStore(_tfsServer, WorkItemStoreFlags.BypassRules)
                : _tfsServer.GetService<WorkItemStore>();

            if (_tfsStore == null)
            {
                Logger.ErrorFormat("Cannot initialize TFS Store");
                throw new Exception("Cannot initialize TFS Store");
            }

            Logger.InfoFormat("Geting TFS Project");
            _tfsProject = _tfsStore.Projects[config.TfsServerConfig.Project];


            Logger.InfoFormat("Getting Team Config");
            var teamConfig = _tfsStore.TeamProjectCollection.GetService<Microsoft.TeamFoundation.ProcessConfiguration.Client.TeamSettingsConfigurationService>()
                .GetTeamConfigurationsForUser(new[] { _tfsProject.Uri.ToString() });

            if (teamConfig != null)
            {
                _teamSettings = teamConfig.First().TeamSettings;
            }

            Logger.InfoFormat("Initializing WorkItems Cache");
            InitWorkItemsCache();

            _nameResolver = InitNameResolver();
        }

        private TfsTeamProjectCollection ConnectToTfsCollection()
        {
            var tfsCredentials = GetTfsCredentials();

            foreach (var credentials in tfsCredentials)
            {
                try
                {
                    Logger.InfoFormat("Connecting to TFS {0} using {1} credentials", _config.TfsServerConfig.CollectionUri, credentials);
                    var tfsServer = new TfsTeamProjectCollection(new Uri(_config.TfsServerConfig.CollectionUri), credentials);
                    tfsServer.EnsureAuthenticated();

                    Logger.InfoFormat("Successfully connected to TFS");

                    return tfsServer;
                }
                catch (Exception ex)
                {
                    Logger.WarnFormat("TFS connection attempt failed.\n Exception: {0}", ex);
                }
            }

            Logger.ErrorFormat("All TFS connection attempts failed");
            throw new Exception("Cannot connect to TFS");
        }

        /// <summary>
        /// Depending on the specific setup of the TFS server, it may or may not accept credentials of specific type. To accommodate for that
        /// without making the configuration more complicated (by making the user explicitly set which type of credentials to use), we just
        /// provide a list of all the relevant credential types we support in order of priority, and when connecting to TFS, we can try them
        /// in order and just go with the first one that succeeds.
        /// 
        /// If ServiceIdentityUsername and ServiceIdentityPasswordFile are set correctly, then we'll try to use those credentials first. Even
        /// then, though, they can be wrapped in different ways (e.g. SimpleWebTokenCredneitals, WindowsCredentials), and different servers
        /// may prefer one over the other.
        ///
        /// We also will try to use basic alternate authentication credentials
        /// 
        /// As a last resort, we always try the default credentials as well
        /// </summary>
        private IEnumerable<TfsClientCredentials> GetTfsCredentials()
        {
            var credentials = new List<TfsClientCredentials>();

            credentials.AddRange(GetOAuthCredentials());
            credentials.AddRange(GetServiceIdentityCredentials());
            credentials.AddRange(GetServiceIdentityPatCredentials());
            credentials.AddRange(GetBasicAuthCredentials());
            credentials.Add(new TfsClientCredentials(new WindowsCredential(false), false));
            credentials.Add(new TfsClientCredentials(true));

            return credentials;
        }

        private IEnumerable<TfsClientCredentials> GetBasicAuthCredentials()
        {
            var usernameAndPassword = GetAltAuthUsernameAndPasswordFromConfig();
            if (usernameAndPassword == null)
            {
                return new List<TfsClientCredentials>();
            }

            NetworkCredential netCred = new NetworkCredential(usernameAndPassword.Item1, usernameAndPassword.Item2);
            BasicAuthCredential basicCred = new BasicAuthCredential(netCred);
            TfsClientCredentials tfsCred = new TfsClientCredentials(basicCred);
            tfsCred.AllowInteractive = false;

            return new List<TfsClientCredentials>
            {
                tfsCred
            };
        }

        private IEnumerable<TfsClientCredentials> GetOAuthCredentials()
        {
            try
            {
                var usernameAndPassword = GetServiceIdentityUsernameAndPasswordFromConfig();

                if (usernameAndPassword == null || 
                    string.IsNullOrEmpty(_config.TfsServerConfig.OAuthClientId) ||
                    string.IsNullOrEmpty(_config.TfsServerConfig.OAuthContext) ||
                    string.IsNullOrEmpty(_config.TfsServerConfig.OAuthResourceId))
                {
                    return new List<TfsClientCredentials>();
                }

                var userCredential = new UserCredential(usernameAndPassword.Item1, usernameAndPassword.Item2);
                var authContext = new AuthenticationContext(_config.TfsServerConfig.OAuthContext);
                var result = authContext.AcquireToken(_config.TfsServerConfig.OAuthResourceId, _config.TfsServerConfig.OAuthClientId, userCredential);
                var oauthToken = new OAuthTokenCredential(result.AccessToken);
                return new List<TfsClientCredentials>()
                {
                    new TfsClientCredentials(oauthToken)
                };
            }
            catch (Exception ex)
            {
                Logger.WarnFormat("Error trying to generate OAuth Token for TFS connection\n{0}", ex);
                return new List<TfsClientCredentials>();
            }
        }

        private IEnumerable<TfsClientCredentials> GetServiceIdentityCredentials()
        {
            var usernameAndPassword = GetServiceIdentityUsernameAndPasswordFromConfig();
            if (usernameAndPassword == null)
            {
                return new List<TfsClientCredentials>();
            }

            return new List<TfsClientCredentials>
            {
                new TfsClientCredentials(
                    new WindowsCredential(
                        new NetworkCredential(usernameAndPassword.Item1, usernameAndPassword.Item2)))
            };
        }

        private Tuple<string, string> GetAltAuthUsernameAndPasswordFromConfig()
        {
            if (string.IsNullOrWhiteSpace(_config.TfsServerConfig.AltAuthUsername)
                || string.IsNullOrWhiteSpace(_config.TfsServerConfig.AltAuthPasswordFile))
            {
                return null;
            }

            if (!File.Exists(_config.TfsServerConfig.AltAuthPasswordFile))
            {
                throw new BadConfigException("AltAuthPasswordFile", "Alt password file doesn't exist");
            }

            return new Tuple<string, string>(
                    _config.TfsServerConfig.AltAuthUsername,
                    DPAPIHelper.ReadDataFromFile(_config.TfsServerConfig.AltAuthPasswordFile, _config.TfsServerConfig.EncryptionScope));
        }

        private Tuple<string,string> GetServiceIdentityUsernameAndPasswordFromConfig()
        {
            if ((string.IsNullOrWhiteSpace(_config.TfsServerConfig.ServiceIdentityPasswordFile) || 
                 string.IsNullOrWhiteSpace(_config.TfsServerConfig.ServiceIdentityUsername)
                ) &&
                _config.TfsServerConfig.ServiceIdentityKeyVaultSecret == null)
            {
                return null;
            }

            var credentialsHelper = new Helpers.CredentialsHelper();
            string password = credentialsHelper.GetPassword(
                _config.TfsServerConfig.ServiceIdentityPasswordFile,
                _config.TfsServerConfig.EncryptionScope,
                _config.TfsServerConfig.ServiceIdentityKeyVaultSecret);

            return new Tuple<string, string>(_config.TfsServerConfig.ServiceIdentityUsername, password);
        }

        private IEnumerable<TfsClientCredentials> GetServiceIdentityPatCredentials()
        {
            if (string.IsNullOrWhiteSpace(_config.TfsServerConfig.ServiceIdentityPatFile) && _config.TfsServerConfig.ServiceIdentityPatKeyVaultSecret == null)
            {
                return new List<TfsClientCredentials>();
            }

            var netCred = new NetworkCredential("", GetPatFromConfig());
            var basicCred = new BasicAuthCredential(netCred);
            var patCred = new TfsClientCredentials(basicCred) { AllowInteractive = false };

            return new List<TfsClientCredentials> { patCred };
        }

        private string GetPatFromConfig()
        {
            if (string.IsNullOrWhiteSpace(_config.TfsServerConfig.ServiceIdentityPatFile) && _config.TfsServerConfig.ServiceIdentityPatKeyVaultSecret == null)
            {
                return null;
            }

            var credentialsHelper = new Helpers.CredentialsHelper();
            return credentialsHelper.GetPassword(
                _config.TfsServerConfig.ServiceIdentityPatFile,
                _config.TfsServerConfig.EncryptionScope,
                _config.TfsServerConfig.ServiceIdentityPatKeyVaultSecret);
        }

        public void AttachFiles(int workItemId, IReadOnlyCollection<MessageAttachmentInfo> messageAttachments)
        {
            if (workItemId <= 0) return;

            try
            {
                WorkItem workItem = _tfsStore.GetWorkItem(workItemId);
                workItem.Open();

                foreach (var attachment in messageAttachments)
                {
                    workItem.Attachments.Add(new Attachment(attachment.FilePath));
                }

                ValidateAndSaveWorkItem(workItem);
            }
            catch (Exception exception)
            {
                Logger.Error(exception.ToString());
            }
        }

        /// <param name="values">The list of fields and their desired values to apply to the work item</param>
        /// <param name="attachments"></param>
        /// <returns>Work item ID of the newly created work item</returns>
        public int CreateWorkItem(Dictionary<string, string> values, MessageAttachmentCollection attachments)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values), "Must supply field values when creating new work item");
            }

            //create a work item
            var workItemType = _tfsProject.WorkItemTypes[_config.TfsServerConfig.WorkItemTemplate];
            var workItem = new WorkItem(workItemType);

            workItem.Open();
            foreach (var key in values.Keys)
            {
                string value = values[key];

                // Resolve current iteration
                if (_teamSettings != null && key == IterationPathFieldKey && value == CurrentIterationSpecialValue)
                {
                    value = _teamSettings.CurrentIterationPath;
                }

                TryApplyFieldValue(workItem, key, value);
            }

            // Workaround for TFS issue - if you change the "Assigned To" field, and then you change the "Activated by" field, the "Assigned To" field reverts
            // to its original setting. To prevent that, we reapply the "Assigned To" field in case it's in the list of values to change.
            if (values.ContainsKey(AssignedToFieldKey))
            {
                TryApplyFieldValue(workItem, AssignedToFieldKey, values[AssignedToFieldKey]);
            }

            foreach (var attachmentPath in attachments.LocalFilePaths)
            {
                workItem.Attachments.Add(new Attachment(attachmentPath));
            }

            ValidateAndSaveWorkItem(workItem);

            CacheWorkItem(workItem);
            return workItem.Id;
        }

        /// <param name="workItemId">The ID of the work item to modify </param>
        /// <param name="comment">Comment to add to description</param>
        /// <param name="commentIsHtml"></param>
        /// <param name="values">List of fields to change</param>
        /// <param name="attachments"></param>
        public void ModifyWorkItem(int workItemId, string comment, bool commentIsHtml, Dictionary<string, string> values, MessageAttachmentCollection attachments)
        {
            if (workItemId <= 0) return;

            var workItem = _tfsStore.GetWorkItem(workItemId);

            workItem.Open();

            if (commentIsHtml)
            {
                workItem.History = EmailBodyProcessingUtils.UpdateEmbeddedImageLinks(comment, attachments.Attachments);
            }
            else
            {
                workItem.History = comment.Replace("\n", "<br>");
            }

            foreach (var key in values.Keys)
            {
                TryApplyFieldValue(workItem, key, values[key]);
            }

            if (attachments != null)
            {
                string GetAttachmentKey(string fileName, long length)
                {
                    return  $"{fileName}|{length}";
                }

                var existingAttachments = new HashSet<string>(workItem.Attachments
                    .OfType<Attachment>()
                    .Select(attachment => GetAttachmentKey(attachment.Name, attachment.Length)));

                foreach (var attachment in attachments.Attachments)
                {
                    var localFileInfo = new FileInfo(attachment.FilePath);
                    string key = GetAttachmentKey(localFileInfo.Name, localFileInfo.Length);

                    // If there's already an attachment with the same file name and size, don't bother re-uploading it
                    // TODO: this may break embedded images for html replies since we update the img tag above to point to a local path we don't upload
                    // However, we haven't confirmed this breaks and replying with an identical image is unlikely, so we ignore this for now
                    if (!existingAttachments.Contains(key))
                    {
                        workItem.Attachments.Add(new Attachment(attachment.FilePath));
                    }
                }
            }

            ValidateAndSaveWorkItem(workItem);

            workItem.Save();
        }

        public IWorkItemFields GetWorkItemFields(int workItemId)
        {
            if (workItemId <= 0) return null;
            var tfsWorkItem = _tfsStore.GetWorkItem(workItemId);
            return new TFSWorkItemFields(tfsWorkItem);
        }
        
        #region Work item caching

        public void CacheWorkItem(int workItemId)
        {
            if (WorkItemsCache.ContainsValue(workItemId)) return; // Work item already cached - nothing to do

            // It is important that we don't just get the conversation ID from the caller and update the cache with the work item
            // ID and conversation ID, because if the work item already exists, the conversation ID will be different (probably shorter
            // than the one the caller currently has)
            // That's why we get the work item from TFS and get the conversation ID from there
            CacheWorkItem(_tfsStore.GetWorkItem(workItemId));
        }

        /// <returns>Sorted List of FieldValue's with ConversationIndex as the key</returns>
        private void InitWorkItemsCache()
        {
            Logger.InfoFormat("Initializing work items cache");

            WorkItemsCache = new SortedList<string, int>();

            //search TFS to get list
            var itemsToCache = _tfsStore.Query(_config.TfsServerConfig.CacheQuery);
            Logger.InfoFormat("{0} items retrieved by TFS cache query", itemsToCache.Count);
            foreach (WorkItem workItem in itemsToCache)
            {
                try
                {
                    CacheWorkItem(workItem);
                }
                catch (Exception ex)
                {
                    Logger.ErrorFormat("Exception caught while caching work item with id {0}\n{1}", workItem.Id, ex);
                }
            }
        }

        private void CacheWorkItem(WorkItem workItem)
        {
            var keyField = _config.WorkItemSettings.ConversationIndexFieldName;

            if (!workItem.Fields.Contains(keyField))
            {
                Logger.WarnFormat("Item {0} doesn't contain the key field {1}. Not caching", workItem.Id, keyField);
                return;
            }

            var keyFieldValue = workItem.Fields[keyField].Value.ToString().Trim();
            Logger.DebugFormat("Work item {0} conversation ID is {1}", workItem.Id, keyFieldValue);
            if (string.IsNullOrEmpty(keyFieldValue))
            {
                Logger.DebugFormat("Problem caching work item {0}. Field '{1}' is empty - using ID instead.", workItem.Id, keyField);
                WorkItemsCache[workItem.Id.ToString(CultureInfo.InvariantCulture)] = workItem.Id;
            }

            WorkItemsCache[keyFieldValue] = workItem.Id;
        }

        #endregion

        public INameResolver GetNameResolver()
        {
            return _nameResolver;
        }

        private static void ValidateAndSaveWorkItem(WorkItem workItem)
        {
            if (!workItem.IsValid())
            {
                var invalidFields = workItem.Validate();

                var sb = new StringBuilder();
                sb.AppendLine("Can't save item because the following fields are invalid: ");
                foreach (Field field in invalidFields)
                {
                    sb.AppendFormat("{0}: '{1}'", field.Name, field.Value).AppendLine();
                }
                Logger.ErrorFormat(sb.ToString());

                return;
            }

            workItem.Save();
        }

        private NameResolver InitNameResolver()
        {
            var workItemType = _tfsProject.WorkItemTypes[_config.TfsServerConfig.WorkItemTemplate];
            var fieldDef = workItemType.FieldDefinitions[_config.TfsServerConfig.NamesListFieldName];
            return new NameResolver(fieldDef.AllowedValues.Cast<string>());
        }

        /// <summary>
        /// Try to apply the value for a specific field
        /// </summary>
        /// <param name="workItem">The work item to which we'll try to apply the value</param>
        /// <param name="key">The field to which the value should be applied</param>
        /// <param name="value">The actual value</param>
        private static void TryApplyFieldValue(WorkItem workItem, string key, string value)
        {
            try
            {
                // Take the default value from the workItem - use empty string if it's null
                var field = workItem.Fields[key];
                var defaultFieldValue = field.Value ?? "";

                if (value == null)
                {
                    Logger.ErrorFormat("Attempting to set the value of {0} to null", key);
                    return;
                }

                if ((field.FieldDefinition.FieldType == FieldType.Html) && (!value.StartsWith("<html")))
                {
                    value = value.Replace("\n", "<br>");
                }

                field.Value = value;

                // If this value is not valid, try to "guess" the value from the allowed values list
                if (!field.IsValid)
                {
                    Logger.WarnFormat("'{0}' is an invalid value for {1}. Trying to find approximate value.", value, key);

                    var approximateValue = GetApproximateAllowedValue(field, value);
                    Logger.InfoFormat("Approximate value is {0}", approximateValue);

                    field.Value = approximateValue;
                }

                // Couldn't approximate the value either - give up
                if (!field.IsValid)
                {
                    Logger.ErrorFormat("Attempt to set field value of {0}; reverting to default {1}", key, defaultFieldValue);

                    field.Value = defaultFieldValue;
                }
            }
            catch (FieldDefinitionNotExistException ex)
            {
                Logger.ErrorFormat("Exception caught while trying to set value of field '{0}'\n{1}", key, ex);
            }
        }

        // If this field has a list of allowed values, returns the first allowed value that begins
        // with 'value'. If not such approximate value is found, returns null.
        private static string GetApproximateAllowedValue(Field field, string value)
        {
            if (!field.HasAllowedValuesList)
            {
                return null;
            }

            var allowedValues = field.AllowedValues.Cast<string>();
            var relevantValues = allowedValues.Where(x => x.StartsWith(value));
            return relevantValues.FirstOrDefault();
        }

        public void Dispose()
        {
            if (_tfsServer != null)
            {
                _tfsServer.Dispose();
            }
        }

        ~TFSWorkItemManager()
        {
            Dispose();
        }

        #region Config validation

        private static void ValidateConfig(Config.InstanceConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            // Temp variable used for shorthand writing below
            var tfsConfig = config.TfsServerConfig;

            ValidateConfigString(tfsConfig.CollectionUri, "TfsServerConfig.CollectionUri");
            ValidateConfigString(tfsConfig.Project, "TfsServerConfig.Project");
            ValidateConfigString(tfsConfig.WorkItemTemplate, "TfsServerConfig.WorkItemTemplate");
            ValidateConfigString(tfsConfig.CacheQuery, "TfsServerConfig.CacheQuery");
            ValidateConfigString(tfsConfig.NamesListFieldName, "TfsServerConfig.NamesListFieldName");
            ValidateConfigString(config.WorkItemSettings.ConversationIndexFieldName,
                                 "WorkItemSettings.ConversationIndexFieldName");
        }

        // ReSharper disable UnusedParameter.Local
        private static void ValidateConfigString(string value, string configValueName)
        // ReSharper restore UnusedParameter.Local
        {
            if (string.IsNullOrEmpty(value)) throw new BadConfigException(configValueName);
        }


        #endregion

        #region Consts

        private const string AssignedToFieldKey = "Assigned To";
        private const string IterationPathFieldKey = "Iteration Path";
        private const string CurrentIterationSpecialValue = "@CurrentIteration";
        #endregion

        private readonly WorkItemStore _tfsStore;
        private readonly Project _tfsProject;
        private readonly Microsoft.TeamFoundation.ProcessConfiguration.Client.TeamSettings _teamSettings;
        private readonly NameResolver _nameResolver;

        private readonly Config.InstanceConfig _config;

        private static readonly ILog Logger = LogManager.GetLogger(typeof(TFSWorkItemManager));
        private readonly TfsTeamProjectCollection _tfsServer;
    }
}
