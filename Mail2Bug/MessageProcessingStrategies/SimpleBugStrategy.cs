using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using log4net;
using Mail2Bug.Email;
using Mail2Bug.WorkItemManagement;

namespace Mail2Bug.MessageProcessingStrategies
{
    public class SimpleBugStrategy : IMessageProcessingStrategy
    {
        private const int TfsTextFieldMaxLength = 255;
        private readonly Config.InstanceConfig _config;
		private readonly IWorkItemManager _workItemManager;
        private readonly AckEmailHandler _ackEmailHandler;
        private readonly MessageToWorkItemMapper _messageToWorkItemMapper;

		public SimpleBugStrategy(Config.InstanceConfig config, IWorkItemManager workItemManager)
        {
            _config = config;
            _workItemManager = workItemManager;
		    _ackEmailHandler = new AckEmailHandler(config);
            _messageToWorkItemMapper = 
                new MessageToWorkItemMapper(
                    _config.EmailSettings.AppendOnlyEmailTitleRegex, 
                    _config.EmailSettings.AppendOnlyEmailBodyRegex,
                    _workItemManager.WorkItemsCache);
        }

        public void ProcessInboxMessage(IIncomingEmailMessage message)
        {
            var workItemId = _messageToWorkItemMapper.GetWorkItemId(message);

            if (!workItemId.HasValue) // thread not found, new work item
            {
                NewWorkItem(message);
                return;
            }

            UpdateWorkItem(message, workItemId.Value);
        }

        private void NewWorkItem(IIncomingEmailMessage message)
        {
            var workItemUpdates = new Dictionary<string, string>();

            InitWorkItemFields(message, workItemUpdates);

        	var workItemId = _workItemManager.CreateWorkItem(workItemUpdates);
            Logger.InfoFormat("Added new work item {0} for message with subject: {1} (conversation index:{2})", 
                workItemId, message.Subject, message.ConversationIndex);

            try
            {
                // Since the work item *has* been created, failures in this stage are not treated as critical
                TryApplyFieldOverrides(message, workItemId);
                ProcessAttachments(message, workItemId);
                
                if (_config.WorkItemSettings.AttachOriginalMessage)
                {
                    string originalMessageFile = message.SaveToFile();
                    _workItemManager.AttachFiles(workItemId, new List<string> {originalMessageFile} );
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("Exception caught while applying settings to work item {0}\n{1}", workItemId, ex);
            }
            _ackEmailHandler.SendAckEmail(message, workItemId.ToString(CultureInfo.InvariantCulture));
        }

        private void InitWorkItemFields(IIncomingEmailMessage message, Dictionary<string, string> workItemUpdates)
    	{
            var resolver = new SpecialValueResolver(message, _workItemManager.GetNameResolver());

    		workItemUpdates["Title"] = resolver.Subject;
            var rawConversationIndex = message.ConversationIndex;
            workItemUpdates[_config.WorkItemSettings.ConversationIndexFieldName] = 
                rawConversationIndex.Substring(0, Math.Min(rawConversationIndex.Length, TfsTextFieldMaxLength));

    		foreach (var defaultFieldValue in _config.WorkItemSettings.DefaultFieldValues)
    		{
    		    workItemUpdates[defaultFieldValue.Field] = resolver.Resolve(defaultFieldValue.Value);
    		}
    	}

        private void TryApplyFieldOverrides(IIncomingEmailMessage message, int workItemId)
        {
            var overrides = new OverridesExtractor(message, _config).GetOverrides();

            if (overrides.Count == 0)
            {
                Logger.DebugFormat("No overrides found. Skipping applying overrides.");
                return;
            }

            try
            {
                Logger.DebugFormat("Overrides found. Calling 'ModifyWorkItem'");
                _workItemManager.ModifyWorkItem(workItemId, "", overrides);
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("Exception caught while trying to apply overrides to work item {0}. Overrides: {1}\n{2}",
                                      workItemId, overrides, ex);
            }
        }

        private void UpdateWorkItem(IIncomingEmailMessage message, int workItemId)
        {
            Logger.InfoFormat("Modifying work item {0} subject: {1}", workItemId, message.Subject);

            var resolver = new SpecialValueResolver(message, _workItemManager.GetNameResolver());
            var workItemUpdates = new Dictionary<string, string> { { "Changed By", resolver.Sender } };

            // Construct the text to be appended
            _workItemManager.ModifyWorkItem(workItemId, message.GetLastMessageText(), workItemUpdates);

            ProcessAttachments(message, workItemId);
        }

        private void ProcessAttachments(IIncomingEmailMessage message, int workItemId)
        {
            var attachmentFiles = SaveAttachments(message);
            _workItemManager.AttachFiles(workItemId, (from object file in attachmentFiles select file.ToString()).ToList());
            attachmentFiles.Delete();
        }

        /// <summary>
        /// Take attachments from the current mail message and put them in a work item
        /// </summary>
        /// <param name="message"></param>
        private static TempFileCollection SaveAttachments(IIncomingEmailMessage message)
        {
            var attachmentFiles = new TempFileCollection();

            foreach (var attachment in message.Attachments)
            {
                var filename = attachment.SaveAttachmentToFile();
                if (filename != null)
                {
                    attachmentFiles.AddFile(filename, false);
                    Logger.InfoFormat("Attachment saved to file {0}", filename);
                }
            }

            return attachmentFiles;
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(SimpleBugStrategy));
    }
}
