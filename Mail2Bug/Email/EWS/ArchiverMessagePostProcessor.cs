using log4net;
using Mail2Bug.ExceptionClasses;
using Microsoft.Exchange.WebServices.Data;

namespace Mail2Bug.Email.EWS
{
    /// <summary>
    /// This post-processor archives messages in designated folders when we are done processing them
    /// </summary>
    class ArchiverMessagePostProcessor : IMessagePostProcessor
    {
        /// <summary>
        /// Constructor - gets the desired archive folders and sets things up
        /// </summary>
        /// <param name="successFolderName">The name of the folder where messages that were successfully processed
        /// would be archived</param>
        /// <param name="failureFolderName">The name of the folder where messages that we failed to process would
        /// be archived</param>
        /// <param name="service">The exchange service - needed to resolve folder names</param>
        public ArchiverMessagePostProcessor(string successFolderName, string failureFolderName, ExchangeService service)
        {
            _successFolderId = GetFolderId(successFolderName, service);
            _failureFolderId = GetFolderId(failureFolderName, service);
        }

        /// <summary>
        /// Process a single message based on whether it was processed successfully or not
        /// </summary>
        public void Process(EWSIncomingMessage message, bool successful)
        {
            var destination = successful ? _successFolderId : _failureFolderId;
            message.MoveMessage(destination);
        }

        private FolderId GetFolderId(string folderName, ExchangeService service)
        {
            var folder = FolderNameResolver.FindFolderByName(folderName, service);
            if (folder == null)
            {
                Logger.ErrorFormat("Can't find archive folder ({0})", folderName);
                throw new MailFolderNotFoundException(folderName);
            }

            return folder.Id;
        }

        private readonly FolderId _successFolderId;
        private readonly FolderId _failureFolderId;

        private static readonly ILog Logger = LogManager.GetLogger(typeof(ArchiverMessagePostProcessor));
    }
}
