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
        private readonly IEnumerable<string> _emailAddresses;
        private readonly IEnumerable<string> _displayNames;

        public RecipientsMailboxManager(ExchangeService connection, IEnumerable<string> emailAddresses, IEnumerable<string> displayNames )
        {
            _service = connection;
            _emailAddresses = emailAddresses;
            _displayNames = displayNames;
        }

        public IEnumerable<IIncomingEmailMessage> ReadMessages()
        {
            var inbox = Folder.Bind(_service, WellKnownFolderName.Inbox);
            var view = new ItemView(Math.Max(inbox.TotalCount,100));
            
            var conditions = new List<SearchFilter>();
            foreach (var name in _displayNames)
            {
                conditions.Add(new SearchFilter.ContainsSubstring(ItemSchema.DisplayTo, name));
                conditions.Add(new SearchFilter.ContainsSubstring(ItemSchema.DisplayCc, name));
            }

            foreach (var address in _emailAddresses)
            {
                conditions.Add(new SearchFilter.ContainsSubstring(EmailMessageSchema.ToRecipients, address));
                conditions.Add(new SearchFilter.ContainsSubstring(EmailMessageSchema.CcRecipients, address));
            }

            var filter = new SearchFilter.SearchFilterCollection(LogicalOperator.Or, conditions);
            var items = inbox.FindItems(filter, view);

            return items
                .Where(item => item is EmailMessage)
                .OrderBy(message => message.DateTimeReceived)
                .Select(message => new EWSIncomingMessage(message as EmailMessage))
                .AsEnumerable();
        }
    }
}
