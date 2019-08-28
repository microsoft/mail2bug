using System.IO;
using log4net;
using Mail2Bug.Helpers;
using Microsoft.Exchange.WebServices.Data;

namespace Mail2Bug.Email.EWS
{
    public class EWSIncomingItemAttachment : IIncomingEmailAttachment
    {
        private readonly ItemAttachment _attachment;

        public EWSIncomingItemAttachment(ItemAttachment attachment)
        {
            _attachment = attachment;
            Logger.DebugFormat("Loading attachment");
            var additionalProperties = new PropertySet { ItemSchema.MimeContent };
            _attachment.Load(additionalProperties);

            Logger.DebugFormat("Attachment name is {0}", _attachment.Name);
        }

        public string SaveAttachmentToFile()
        {
            return SaveAttachmentToFile(FileUtils.GetValidFileName(_attachment.Name, "eml", Path.GetTempPath()));
        }

        public string SaveAttachmentToFile(string filename)
        {
            Logger.DebugFormat("Saving attachment named '{0}' to file {1}", _attachment.Name ?? "", filename);

            using (var fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                var contents = _attachment.Item.MimeContent.Content;
                Logger.DebugFormat("Attachment '{0}' is {1} bytes long", _attachment.Name, contents.Length);
                fs.Write(contents, 0, contents.Length);
            }

            return filename;
        }

        public string ContentId
        {
            get => _attachment.ContentId;
            set => _attachment.ContentId = value;
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof (EWSIncomingItemAttachment));
    }
}
