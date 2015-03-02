using System.Text;
using log4net;
using Mail2Bug.Email.EWS;

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
        public void SendAckEmail(IIncomingEmailMessage originalMessage, string bugId)
        {
            // Don't send ack emails if it's disabled in configuration or if we're in simulation mode
            if (!_config.EmailSettings.SendAckEmails || _config.TfsServerConfig.SimulationMode)
            {
                Logger.DebugFormat("Ack emails disabled in configuration - skipping");
                return;
            }

            var ewsMessage = originalMessage as EWSIncomingMessage;
            if (ewsMessage != null)
            {
                HandleEWSMessage(ewsMessage, bugId);
            }
        }

        private void HandleEWSMessage(EWSIncomingMessage originalMessage, string bugId)
        {
            originalMessage.Reply(GetReplyContents(bugId), _config.EmailSettings.AckEmailsRecipientsAll);
        }

        private string GetReplyContents(string bugId)
        {
            var bodyBuilder = new StringBuilder();
            bodyBuilder.Append(_config.EmailSettings.GetReplyTemplate());
            bodyBuilder.Replace("[BUGID]", bugId);
            bodyBuilder.Replace("[TFSCollectionUri]", _config.TfsServerConfig.CollectionUri);
            return bodyBuilder.ToString();
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(AckEmailHandler));
    }
}
