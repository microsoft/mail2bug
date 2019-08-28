using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;
using Mail2Bug.Email;
using Mail2Bug.Helpers;
using Mail2Bug.WorkItemManagement;

namespace Mail2Bug.MessageProcessingStrategies
{
    public class SimpleBugStrategy : IMessageProcessingStrategy, IDisposable
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
                    _workItemManager.WorkItemsCache,
                    _config.EmailSettings.UseConversationGuidOnly);
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

            using (var attachments = SaveAttachments(message))
            {
                InitWorkItemFields(message, workItemUpdates, attachments);

                int workItemId = _workItemManager.CreateWorkItem(workItemUpdates, attachments);
                Logger.InfoFormat("Added new work item {0} for message with subject: {1} (conversation index:{2})", 
                    workItemId, message.Subject, message.ConversationId);

                try
                {
                    // Since the work item *has* been created, failures in this stage are not treated as critical
                    var overrides = new OverridesExtractor(_config).GetOverrides(message);
                    TryApplyFieldOverrides(overrides, workItemId);
                
                    if (_config.WorkItemSettings.AttachOriginalMessage)
                    {
                        AttachMessageToWorkItem(message, workItemId, "OriginalMessage");
                    }
                }
                catch (Exception ex)
                {
                    Logger.ErrorFormat("Exception caught while applying settings to work item {0}\n{1}", workItemId, ex);
                }

                var workItem = _workItemManager.GetWorkItemFields(workItemId);
                _ackEmailHandler.SendAckEmail(message, workItem);
            }
        }

        private void AttachMessageToWorkItem(IIncomingEmailMessage message, int workItemId, string prefix)
        {
            using (var tfc = new TempFileCollection())
            {
                var fileName = string.Format("{0}_{1}_{2}.eml", prefix, DateTime.Now.ToString("yyyyMMdd_hhmmss"), new Random().Next());
                var filePath = Path.Combine(Path.GetTempPath(), fileName);
                
                message.SaveToFile(filePath);

                // Remove the file once we're done attaching it
                tfc.AddFile(filePath, false);

                _workItemManager.AttachFiles(workItemId, new List<MessageAttachmentInfo> { new MessageAttachmentInfo(filePath, string.Empty) });
            }
        }

        private void InitWorkItemFields(IIncomingEmailMessage message, Dictionary<string, string> workItemUpdates, MessageAttachmentCollection attachments)
    	{
            var resolver = new SpecialValueResolver(message, _workItemManager.GetNameResolver());

    		workItemUpdates["Title"] = resolver.Subject;
            var rawConversationIndex = message.ConversationId;
            workItemUpdates[_config.WorkItemSettings.ConversationIndexFieldName] = 
                rawConversationIndex.Substring(0, Math.Min(rawConversationIndex.Length, TfsTextFieldMaxLength));

            bool enableImgUpdating = _config.WorkItemSettings.EnableExperimentalHtmlFeatures;
            foreach (var defaultFieldValue in _config.WorkItemSettings.DefaultFieldValues)
    		{
    		    var result = resolver.Resolve(defaultFieldValue.Value);
                if (enableImgUpdating && message.IsHtmlBody && defaultFieldValue.Value == SpecialValueResolver.RawMessageBodyKeyword)
                {
                    result = EmailBodyProcessingUtils.UpdateEmbeddedImageLinks(result, attachments.Attachments);
                }

                workItemUpdates[defaultFieldValue.Field] = result;
            }
    	}

        private void TryApplyFieldOverrides(Dictionary<string, string> overrides, int workItemId)
        {
            if (overrides.Count == 0)
            {
                Logger.DebugFormat("No overrides found. Skipping applying overrides.");
                return;
            }

            try
            {
                Logger.DebugFormat("Overrides found. Calling 'ModifyWorkItem'");
                _workItemManager.ModifyWorkItem(workItemId, "", false, overrides, null);
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
            var workItemUpdates = new Dictionary<string, string>();

            if (_config.WorkItemSettings.OverrideChangedBy)
            {
                workItemUpdates["Changed By"] = resolver.Sender;
            }

            bool isHtmlEnabled = _config.WorkItemSettings.EnableExperimentalHtmlFeatures;
            bool commentIsHtml = message.IsHtmlBody && isHtmlEnabled;

            string lastMessageText = message.GetLastMessageText(isHtmlEnabled);
            if (_config.WorkItemSettings.ApplyOverridesDuringUpdate)
            {
                // OverrideExtractor can't currently handle HTML input, so we need to make sure we pass it the plain text version
                string lastMessagePlainText = commentIsHtml
                    ? message.GetLastMessageText(enableExperimentalHtmlFeatures: false)
                    : lastMessageText;

                var extractor = new OverridesExtractor(_config);
                var overrides = extractor.GetOverrides(lastMessagePlainText);

                Logger.DebugFormat("Found {0} overrides for update message", overrides.Count);

                overrides.ToList().ForEach(x => workItemUpdates[x.Key] = x.Value);
            }

            using (var attachments = SaveAttachments(message))
            {
                _workItemManager.ModifyWorkItem(workItemId, lastMessageText, commentIsHtml, workItemUpdates, attachments);
            }

            if (_config.WorkItemSettings.AttachUpdateMessages)
            {
                AttachMessageToWorkItem(message, workItemId, "ReplyMessage");
            }
        }

        /// <summary>
        /// Take attachments from the current mail message and put them in a work item
        /// </summary>
        /// <param name="message"></param>
        private static MessageAttachmentCollection SaveAttachments(IIncomingEmailMessage message)
        {
            var result = new MessageAttachmentCollection();
            foreach (var attachment in message.Attachments)
            {
                var filename = attachment.SaveAttachmentToFile();
                if (filename != null)
                {
                    result.Add(filename, attachment.ContentId);
                    Logger.InfoFormat("Attachment saved to file {0}", filename);
                }
            }

            return result;
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(SimpleBugStrategy));

        public void Dispose()
        {
            DisposeUtils.DisposeIfDisposable(_workItemManager);
        }
    }
}
