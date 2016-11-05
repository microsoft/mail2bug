using System.Collections.Generic;
using System.Text;
using log4net;
using Mail2Bug.Email.EWS;
using Mail2Bug.WorkItemManagement;

namespace Mail2Bug.Email
{
    class AckEmailHandler
    {
       	private readonly Config.InstanceConfig _config;

        public AckEmailHandler(Config.InstanceConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Send mail announcing receipt of new ticket
        /// </summary>
        public void SendAckEmail(IIncomingEmailMessage originalMessage, SimpleWorkItem workItem)
        {
            // Don't send ack emails if it's disabled in configuration
            if (!_config.EmailSettings.SendAckEmails)
            {
                Logger.DebugFormat("Ack emails disabled in configuration - skipping");
                return;
            }

            var ewsMessage = originalMessage as EWSIncomingMessage;
            if (ewsMessage != null)
            {
                HandleEWSMessage(ewsMessage, workItem);
            }
        }

        private void HandleEWSMessage(EWSIncomingMessage originalMessage, SimpleWorkItem workItem)
        {
            originalMessage.Reply(GetReplyContents(workItem), _config.EmailSettings.AckEmailsRecipientsAll);
        }

        private string GetReplyContents(SimpleWorkItem workItem)
        {
            var bodyBuilder = new StringBuilder();
            bodyBuilder.Append(_config.EmailSettings.GetReplyTemplate());
            bodyBuilder.Replace("[BUGID]", workItem?.Id.ToString());
            bodyBuilder.Replace("[BugTitle]", workItem?.Title);
            bodyBuilder.Replace("[BugOwner]", string.IsNullOrEmpty(workItem?.AssignedTo) ? "Unassigned" : workItem.AssignedTo);
            bodyBuilder.Replace("[BugType]", _config.TfsServerConfig.WorkItemTemplate);
            bodyBuilder.Replace("[TFSCollectionUri]", _config.TfsServerConfig.CollectionUri);
            bodyBuilder.Replace("[TFSProject]", _config.TfsServerConfig.Project);
            bodyBuilder.Replace("[Mail2BugAlias]", _config.EmailSettings.Recipients?.Replace(';', '/'));
            return bodyBuilder.ToString();
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(AckEmailHandler));
    }
}
