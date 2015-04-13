using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Exchange.WebServices.Data;

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
        private readonly ExchangeService _service;
        private readonly IEnumerable<string> _recipients;
        private readonly IMessagePostProcessor _postProcessor;

        public RecipientsMailboxManager(ExchangeService connection, IEnumerable<string> recipients, IMessagePostProcessor postProcessor)
        {
            _service = connection;
            _recipients = recipients;
            _postProcessor = postProcessor;
        }

        public IEnumerable<IIncomingEmailMessage> ReadMessages()
        {
            var inbox = Folder.Bind(_service, WellKnownFolderName.Inbox);
            var view = new ItemView(Math.Max(inbox.TotalCount,100));

            var items = inbox.FindItems(view);

            return items
                .Where(ShouldConsiderItem)
                .OrderBy(message =>
                {   
                    message.Load(new PropertySet(ItemSchema.DateTimeReceived));
                    return message.DateTimeReceived; 
                })
                .Select(message => new EWSIncomingMessage(message as EmailMessage))
                .AsEnumerable();
        }

        public void OnProcessingFinished(IIncomingEmailMessage message, bool successful)
        {
            _postProcessor.Process((EWSIncomingMessage)message, successful);
        }

        private bool ShouldConsiderItem(Item item)
        {
            // Consider only email messages
            var message = item as EmailMessage;
            
            if (message == null)
            {
                return false;
            }

            // If no recipients were mentioned, it means process all incoming emails
            if (!_recipients.Any())
            {
                return true;
            }

            // Load the properties we're going to use for evaluating the message
            message.Load(new PropertySet(
                    EmailMessageSchema.ToRecipients,
                    EmailMessageSchema.CcRecipients
                ));

            // If the recipient is in either the To or CC lines, then this message should be considered
            return _recipients.Any(recipient =>
                EmailAddressesMatch(message.ToRecipients, recipient) ||
                EmailAddressesMatch(message.CcRecipients, recipient));
        }

        private bool EmailAddressesMatch(IEnumerable<EmailAddress> emailAddresses, string recipient)
        {
            if (emailAddresses == null)
            {
                return false;
            }

            return
                emailAddresses.Any(
                    address =>
                        address.Address.Equals(recipient, StringComparison.InvariantCultureIgnoreCase) ||
                        address.Name.Equals(recipient, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
