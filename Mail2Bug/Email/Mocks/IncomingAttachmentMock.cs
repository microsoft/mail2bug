using System;
using System.IO;

namespace Mail2Bug.Email.Mocks
{
    class IncomingAttachmentMock : IIncomingEmailAttachment
    {
        public IncomingAttachmentMock()
        {
            ExceptionToThrow = null;
        }

        public IncomingAttachmentMock(int dataSize) : this()
        {
            Data = new byte[dataSize];
            Rand.NextBytes(Data);
        }

        public string SaveAttachmentToFile()
        {
            return SaveAttachmentToFile(Path.GetTempFileName());
        }

        public string SaveAttachmentToFile(string filename)
        {
            if (ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }

            using (var fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                fs.Write(Data, 0, Data.Length);
            }

            return filename;
        }

        public Exception ExceptionToThrow { get; set; }
        public byte[] Data = new byte[1];

        private static readonly Random Rand = new Random();
    }
}
