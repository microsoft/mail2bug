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
        private readonly IMessagePostProcessor _postProcessor;
        private readonly bool _useConversationGuidOnly;

        public FolderMailboxManager(ExchangeService connection, string incomingFolder, IMessagePostProcessor postProcessor, bool useConversationGuidOnly)
        {
            _service = connection;
            _mailFolder = incomingFolder;
            _postProcessor = postProcessor;
            _useConversationGuidOnly = useConversationGuidOnly;
        }

        public IEnumerable<IIncomingEmailMessage> ReadMessages()
        {
            var folder = FolderNameResolver.FindFolderByName(_mailFolder, _service);

            if (folder == null)
            {
                Logger.ErrorFormat("Couldn't find incoming mail folder ({0})", _mailFolder);
                throw new MailFolderNotFoundException(_mailFolder);
            }

            if (folder.TotalCount == 0)
            {
                Logger.DebugFormat("No items found in folder '{0}'. Returning empty list.", _mailFolder);
                return new List<IIncomingEmailMessage>();
            }

            var items = folder.FindItems(new ItemView(folder.TotalCount)).OrderBy(x => x.DateTimeReceived);

            return items
                .Where(item => item is EmailMessage)
                .OrderBy(message => message.DateTimeReceived)
                .Select(message => new EWSIncomingMessage(message as EmailMessage, this._useConversationGuidOnly))
                .AsEnumerable();
        }

        public void OnProcessingFinished(IIncomingEmailMessage message, bool successful)
        {
            _postProcessor.Process((EWSIncomingMessage)message, successful);
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(FolderMailboxManager));
    }
}
