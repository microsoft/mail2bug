using System.IO;
using log4net;
using Mail2Bug.Helpers;

namespace Mail2Bug.Email.EWS
{
    class EWSIncomingFileAttachment : IIncomingEmailAttachment
    {
        private readonly Microsoft.Exchange.WebServices.Data.FileAttachment _attachment;
        private string _contentId;

        public EWSIncomingFileAttachment(Microsoft.Exchange.WebServices.Data.FileAttachment attachment)
        {
            _attachment = attachment;
            _contentId = attachment.ContentId;
        }

        public bool IsInline { get { return _attachment.IsInline; } }
        public string ContentId { get { return _contentId; } }
        public string ContentType { get { return _attachment.ContentType; } }
        public byte[] Content { get { return _attachment.Content; } }

        public void SetContentId(string contentId)
        {
            _contentId = contentId;
        }

        public string SaveAttachmentToFile()
        {
            var baseFilename = Path.GetFileNameWithoutExtension(_attachment.Name);
            var extension = Path.GetExtension(_attachment.Name);
            return SaveAttachmentToFile(FileUtils.GetValidFileName(baseFilename, extension, Path.GetTempPath()));
        }

        public string SaveAttachmentToFile(string filename)
        {
            Logger.DebugFormat("Saving attachment named '{0}' to file {1}", _attachment.Name ?? "", filename);

            _attachment.Load(filename);
            return filename;
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(EWSIncomingFileAttachment));
    }
}
