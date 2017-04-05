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
        public void SendAckEmail(IIncomingEmailMessage originalMessage, IWorkItemFields workItemFields)
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
                HandleEWSMessage(ewsMessage, workItemFields);
            }
        }

        private void HandleEWSMessage(EWSIncomingMessage originalMessage, IWorkItemFields workItemFields)
        {
            var replyTemplate = new AckEmailTemplate(_config.EmailSettings.GetReplyTemplate());
            var replyBody = replyTemplate.Apply(workItemFields, _config);
            originalMessage.Reply(replyBody, _config.EmailSettings.AckEmailsRecipientsAll);
        }
        
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AckEmailHandler));
    }
}
