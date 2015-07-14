using System;
using System.Collections.Generic;
using System.Linq;

namespace Mail2Bug.Email.EWS
{
    /// <summary>
    /// This imiplementation of IMailboxManager monitors the inbox of an exchange user, and retrieves
    /// only messages that have a specific alias in the 'To' or 'CC' lines
    /// It can be initialized with a list of displayNames, in which case, if any of the displayNames are in the 'To'
    /// or 'CC' lines, the message will be retrieved
    /// </summary>
    public class RecipientsMailboxManager : IMailboxManager
    {
        private readonly IMessagePostProcessor _postProcessor;
        private readonly RecipientsMailboxManagerRouter _router;
        private readonly int _clientId;

        public RecipientsMailboxManager(RecipientsMailboxManagerRouter router, IEnumerable<string> recipients, IMessagePostProcessor postProcessor)
        {
            _router = router;
            _postProcessor = postProcessor;

            _clientId = _router.RegisterMailbox(m => ShouldConsiderMessage(m, recipients.ToArray()));
        }

        public IEnumerable<IIncomingEmailMessage> ReadMessages()
        {
            return _router.GetMessages(_clientId);
        }

        public void OnProcessingFinished(IIncomingEmailMessage message, bool successful)
        {
            _postProcessor.Process((EWSIncomingMessage)message, successful);
        }

        private static bool ShouldConsiderMessage(IIncomingEmailMessage message, string[] recipients)
        {
            if (message == null)
            {
                return false;
            }

            // If no recipients were mentioned, it means process all incoming emails
            if (!recipients.Any())
            {
                return true;
            }

            // If the recipient is in either the To or CC lines, then this message should be considered
            return recipients.Any(recipient =>
                EmailAddressesMatch(message.ToAddresses, recipient) ||
                EmailAddressesMatch(message.ToNames, recipient) ||
                EmailAddressesMatch(message.CcAddresses, recipient) ||
                EmailAddressesMatch(message.CcNames, recipient));
        }

        private static bool EmailAddressesMatch(IEnumerable<string> emailAddresses, string recipient)
        {
            return emailAddresses != null && 
                emailAddresses.Any(address =>
                    address != null && address.Equals(recipient, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
