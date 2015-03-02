using System.Collections.Generic;
using log4net;
using Mail2Bug.Email;
using Mail2Bug.WorkItemManagement;

namespace Mail2Bug.MessageProcessingStrategies
{
    /// <summary>
    /// This processing strategy is intended to only apply updates to existing work items
    /// It will apply the default values from the config, as well as any overrides in the body
    /// of the latest message to the existing work item.
    /// </summary>
    public class UpdateItemMetadataStrategy : IMessageProcessingStrategy
    {
    	private readonly Config.InstanceConfig _config;
		private readonly IWorkItemManager _workItemManager;
        private readonly MessageToWorkItemMapper _messageToWorkItemMapper;

        public UpdateItemMetadataStrategy(Config.InstanceConfig config, IWorkItemManager workItemManager)
        {
            _config = config;
            _workItemManager = workItemManager;
            _messageToWorkItemMapper =
                new MessageToWorkItemMapper(
                    _config.EmailSettings.AppendOnlyEmailTitleRegex,
                    _config.EmailSettings.AppendOnlyEmailBodyRegex,
                    _workItemManager.WorkItemsCache);
        }

        public void ProcessInboxMessage(IIncomingEmailMessage message)
        {
            var workItemId = _messageToWorkItemMapper.GetWorkItemId(message);

            if (!workItemId.HasValue) 
            {
                // Since this strategy only handles updating existing work items, if
                // we can't find a work item ID, just do nothing
                Logger.WarnFormat("UpdateItemMetadataStrategy: Couldn't find work item ID for message titled {0}", message.Subject);
                return;
            }

            UpdateWorkItem(message, workItemId.Value);
        }

        private void UpdateWorkItem(IIncomingEmailMessage message, int workItemId)
        {
            var resolver = new SpecialValueResolver(message, _workItemManager.GetNameResolver());

            var workItemUpdates = new Dictionary<string, string>();
            
            workItemUpdates["Changed By"] = resolver.Sender;

            // Set all the fields from the defaults 
            foreach (var defaultFieldValue in _config.WorkItemSettings.DefaultFieldValues)
            {
                workItemUpdates[defaultFieldValue.Field] = resolver.Resolve(defaultFieldValue.Value);
            }

            // Apply overrides
            var overrides = new OverridesExtractor(message, _config).GetOverrides();
            foreach (var item in overrides)
            {
                workItemUpdates[item.Key] = item.Value;
            }

            // Modify the work item
            _workItemManager.ModifyWorkItem(workItemId, message.GetLastMessageText(), workItemUpdates);
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(UpdateItemMetadataStrategy));
    }
}
