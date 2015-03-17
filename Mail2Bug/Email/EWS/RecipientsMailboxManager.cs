using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private readonly string _recipientsQueryString;

        public RecipientsMailboxManager(ExchangeService connection, IEnumerable<string> recipients)
        {
            _service = connection;
            _recipientsQueryString = BuildRecipientsQueryString(recipients);
        }

        public IEnumerable<IIncomingEmailMessage> ReadMessages()
        {
            var inbox = Folder.Bind(_service, WellKnownFolderName.Inbox);
            var view = new ItemView(Math.Max(inbox.TotalCount,100));

            var items = inbox.FindItems(_recipientsQueryString, view);

            return items
                .Where(item => item is EmailMessage)
                .OrderBy(message => message.DateTimeReceived)
                .Select(message => new EWSIncomingMessage(message as EmailMessage))
                .AsEnumerable();
        }

        private static string BuildRecipientsQueryString(IEnumerable<string> recipients)
        {
            var queryString = new StringBuilder();
            foreach (var name in recipients)
            {
                if (queryString.Length != 0)
                {
                    queryString.Append(" OR ");
                }
                queryString.AppendFormat("participants:\"{0}\"", name.ToLower());
            }

            return queryString.ToString();
        }
    }
}
