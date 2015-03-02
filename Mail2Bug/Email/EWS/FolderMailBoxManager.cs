using System.Collections.Generic;
using System.Linq;
using log4net;
using Mail2Bug.ExceptionClasses;
using Microsoft.Exchange.WebServices.Data;

namespace Mail2Bug.Email.EWS
{
    /// <summary>
    /// This implementation of IMailboxManager monitors a specific folder belonging to an exchange
    /// user. All messages coming into the folder will be retrieved via ReadMessages
    /// </summary>
    public class FolderMailboxManager : IMailboxManager
    {
        private readonly ExchangeService _service;
        private readonly string _mailFolder;

        public FolderMailboxManager(ExchangeService connection, string incomingFolder)
        {
            _service = connection;
            _mailFolder = incomingFolder;
        }

        public IEnumerable<IIncomingEmailMessage> ReadMessages()
        {
            var folder = FindFolderByName(_mailFolder);
            if (folder.TotalCount == 0)
            {
                Logger.DebugFormat("No items found in folder '{0}'. Returning empty list.", _mailFolder);
                return new List<IIncomingEmailMessage>();
            }

            var items = folder.FindItems(new ItemView(folder.TotalCount)).OrderBy(x => x.DateTimeReceived);

            return items
                .Where(item => item is EmailMessage)
                .OrderBy(message => message.DateTimeReceived)
                .Select(message => new EWSIncomingMessage(message as EmailMessage))
                .AsEnumerable();
        }

        public Folder FindFolderByName(string mailFolder)
        {
            Logger.DebugFormat("Looking for folder named '{0}'", mailFolder);
            // Look for the folder under the mailbox root
            var rootFolder = Folder.Bind(_service, WellKnownFolderName.MsgFolderRoot);

            // Folder name should be equal to 'mailFolder'
            var folderFilter = new SearchFilter.IsEqualTo(FolderSchema.DisplayName, mailFolder);

            // No need to look for more than one folder (can't have more than one folder with the exact same name)
            var findFoldersResults = rootFolder.FindFolders(folderFilter, new FolderView(1));

            if (!findFoldersResults.Any())
            {
                throw new MailFolderNotFoundException(mailFolder);
            }

            Logger.DebugFormat("Found folder {0} ({1} matching folder items)", mailFolder, findFoldersResults.Count());

            var folder = findFoldersResults.First();
            return folder;
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(FolderMailboxManager));
    }
}
