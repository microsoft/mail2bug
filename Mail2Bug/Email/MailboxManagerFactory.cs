using System.Collections;
using System.Collections.Generic;
using Mail2Bug.Email.EWS;
using Mail2Bug.ExceptionClasses;
using Mail2Bug.Helpers;

namespace Mail2Bug.Email
{
    /// <summary>
    ///  The goal of the MailboxManagerFactory is to separate concerns from the Mail2BugEngine - this way it does not need
    /// to be aware of the specific implementation of the EMail layer, as long as it supports the IMailboxManager interface
    /// </summary>
    class MailboxManagerFactory
    {
        public static IMailboxManager CreateMailboxManager(Config.EmailSettings emailSettings)
        {
            var credentials = new EWSConnectionFactory.Credentials
            {
                EmailAddress = emailSettings.EWSMailboxAddress,
                UserName = emailSettings.EWSUsername,
                Password = DPAPIHelper.ReadDataFromFile(emailSettings.EWSPasswordFile)
            };

            switch (emailSettings.ServiceType)
            {
                // We used to support DotMapi as well, but that's deprecated now. Nevertheless, we may want to support
                // other mail providers in the future.
                case Config.EmailSettings.MailboxServiceType.EWSByFolder:
                    return new FolderMailboxManager(
                        ConnectionFactory.GetConnection(credentials), 
                        emailSettings.IncomingFolder);

                case Config.EmailSettings.MailboxServiceType.EWSByRecipients:
                    return new RecipientsMailboxManager(
                        ConnectionFactory.GetConnection(credentials),
                        ParseDelimitedList(emailSettings.RecipientEmailAddresses, ';'),
                        ParseDelimitedList(emailSettings.RecipientDisplayNames, ';'));

                default:
                    throw new BadConfigException(
                        "EmailSettings.ServiceType",
                        string.Format("Invalid mailbox service type defined in config ({0})", emailSettings.ServiceType));
            }
        }

        private static IEnumerable<string> ParseDelimitedList(string text, char delimiter)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new List<string>();
            }

            return text.Split(delimiter);
        }

        // Enable connection caching for performance improvement when hosting multiple instances
        private static readonly EWSConnectionFactory ConnectionFactory = new EWSConnectionFactory(true);
    }
}
