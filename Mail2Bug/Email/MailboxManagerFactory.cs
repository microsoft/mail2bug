using System.Collections.Generic;
using Mail2Bug.Email.EWS;
using Mail2Bug.ExceptionClasses;
using Mail2Bug.Helpers;
using Microsoft.Exchange.WebServices.Data;

namespace Mail2Bug.Email
{
    /// <summary>
    ///  The goal of the MailboxManagerFactory is to separate concerns from the Mail2BugEngine - this way it does not need
    /// to be aware of the specific implementation of the EMail layer, as long as it supports the IMailboxManager interface
    /// </summary>
    class MailboxManagerFactory
    {
        public MailboxManagerFactory(EWSConnectionManger connectionManger)
        {
            _connectionManger = connectionManger;
        }

        public IMailboxManager CreateMailboxManager(Config.EmailSettings emailSettings)
        {
            var credentials = new EWSConnectionManger.Credentials
            {
                EmailAddress = emailSettings.EWSMailboxAddress,
                UserName = emailSettings.EWSUsername,
                Password = DPAPIHelper.ReadDataFromFile(emailSettings.EWSPasswordFile)
            };

            var exchangeService = _connectionManger.GetConnection(credentials);
            var postProcessor = GetPostProcesor(emailSettings, exchangeService.Service);

            switch (emailSettings.ServiceType)
            {
                case Config.EmailSettings.MailboxServiceType.EWSByFolder:
                    return new FolderMailboxManager(
                        exchangeService.Service, 
                        emailSettings.IncomingFolder,
                        postProcessor);

                case Config.EmailSettings.MailboxServiceType.EWSByRecipients:

                    return new RecipientsMailboxManager(
                        exchangeService.Router,
                        ParseDelimitedList(emailSettings.Recipients, ';'),
                        postProcessor);

                default:
                    throw new BadConfigException(
                        "EmailSettings.ServiceType",
                        string.Format("Invalid mailbox service type defined in config ({0})", emailSettings.ServiceType));
            }
        }

        private static IMessagePostProcessor GetPostProcesor(Config.EmailSettings emailSettings, ExchangeService service)
        {
            if (string.IsNullOrEmpty(emailSettings.CompletedFolder) || string.IsNullOrEmpty(emailSettings.ErrorFolder))
            {
                return new DeleterMessagePostProcessor();
            }

            return new ArchiverMessagePostProcessor(emailSettings.CompletedFolder, emailSettings.ErrorFolder, service);
        }

        private static IEnumerable<string> ParseDelimitedList(string text, char delimiter)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new string[0];
            }

            return text.Split(delimiter);
        }

        // Enable connection caching for performance improvement when hosting multiple instances
        private readonly EWSConnectionManger _connectionManger;
    }
}
