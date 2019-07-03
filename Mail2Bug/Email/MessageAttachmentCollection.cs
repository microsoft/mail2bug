using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;

namespace Mail2Bug.Email
{
    /// <summary>
    /// Collection of Exchange email attachments that have been downloaded locally
    /// </summary>
    public class MessageAttachmentCollection : IDisposable
    {
        private readonly List<MessageAttachmentInfo> _attachments;
        private readonly TempFileCollection _tempFileCollection;

        public IReadOnlyCollection<MessageAttachmentInfo> Attachments => _attachments;
        public IEnumerable<string> LocalFilePaths => _attachments.Select(a => a.FilePath);

        public MessageAttachmentCollection()
        {
            _attachments = new List<MessageAttachmentInfo>();
            _tempFileCollection = new TempFileCollection();
        }

        public void Add(string localFilePath, string contentId)
        {
            _attachments.Add(new MessageAttachmentInfo(localFilePath, contentId));
            _tempFileCollection.AddFile(localFilePath, keepFile: false);
        }

        public void DeleteLocalFiles()
        {
            _tempFileCollection.Delete();
        }

        public void Dispose()
        {
            DeleteLocalFiles();
        }
    }
}