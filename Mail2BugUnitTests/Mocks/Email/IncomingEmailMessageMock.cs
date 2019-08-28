using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;
using Mail2Bug.Email;
using Mail2Bug.TestHelpers;

namespace Mail2BugUnitTests.Mocks.Email
{
    public class IncomingEmailMessageMock : IIncomingEmailMessage
    {
        static IncomingEmailMessageMock()
        {
            _seed = (int) (DateTime.Now.Ticks%int.MaxValue);
            Logger.InfoFormat("IncomingEmailMessageMock: Using Seed {0}", _seed);

            Rand = new Random(_seed);

        }

        public static IncomingEmailMessageMock CreateWithRandomData()
        {
            return CreateWithRandomData(false);
        }

        public static IncomingEmailMessageMock CreateWithRandomData(bool withAttachments)
        {
            int numAttachments = withAttachments ? Rand.Next(0, 10) : 0;
            return CreateWithRandomData(numAttachments);
        }

        public static IncomingEmailMessageMock CreateWithRandomData(int numAttachments)
        {
            var mock = new IncomingEmailMessageMock
                {
                    Subject = RandomDataHelper.GetSubject(_seed++),
                    RawBody = RandomDataHelper.GetBody(_seed++),
                    PlainTextBody = RandomDataHelper.GetBody(_seed++),
                    ConversationId = RandomDataHelper.GetConversationId(_seed++),
                    ConversationTopic = RandomDataHelper.GetSubject(_seed++),
                    SenderName = RandomDataHelper.GetName(_seed++),
                    SenderAlias = RandomDataHelper.GetAlias(_seed++)
                };
            mock.SenderAddress = mock.SenderAlias + "@blah.com";
            mock.ToAddresses = GetRandomAliasList(Rand.Next(1, 30));
            mock.CcAddresses = GetRandomAliasList(Rand.Next(0, 30));
            mock.ToNames = GetRandomNamesList(mock.ToAddresses.Count());
            mock.CcNames = GetRandomNamesList(mock.CcAddresses.Count());
            mock.SentOn = new DateTime(Rand.Next(2012, 2525), Rand.Next(1, 12), Rand.Next(1, 28));
            mock.ReceivedOn = new DateTime(Rand.Next(2012, 2525), Rand.Next(1, 12), Rand.Next(1, 28));
            mock.IsHtmlBody = false; // this isn't html unless the creator specifically makes it so

            var attachments = new List<IIncomingEmailAttachment>(numAttachments);
            for (var i = 0; i < numAttachments; i++)
            {
                attachments.Add(new IncomingAttachmentMock(Rand.Next(1, 100000)));
            }

            mock.Attachments = attachments;
            
            return mock;
        }

        public string Subject { get; set; }
        public string RawBody { get; set; }
        public string PlainTextBody { get; set; }
        public string ConversationId { get; set; }
        public string ConversationTopic { get; set; }
        public string SenderName { get; set; }
        public string SenderAlias { get; set; }
        public string SenderAddress { get; private set; }
        public IEnumerable<string> ToAddresses { get; set; }
        public IEnumerable<string> CcAddresses { get; set; }
        public IEnumerable<string> ToNames { get; set; }
        public IEnumerable<string> CcNames { get; set; }
        public DateTime SentOn { get; set; }
        public DateTime ReceivedOn { get; set; }
        public bool IsHtmlBody { get; set; }
        public string Location { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public string SaveToFile()
        {
            return SaveToFile(Path.GetTempFileName());
        }

        public string SaveToFile(string filename)
        {
            if (ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }

            using (var sw = new StreamWriter(new FileStream(filename, FileMode.Create, FileAccess.Write)))
            {
                sw.WriteLine("Subject: " + Subject);
                sw.WriteLine();
                sw.WriteLine(RawBody);
            }

            return filename;
        }

        public string GetLastMessageText(bool enableExperimentalHtmlFeatures)
        {
            return EmailBodyProcessingUtils.GetLastMessageText(this, enableExperimentalHtmlFeatures);
        }

        public void CopyToFolder(string destinationFolder)
        {
            CopyToFolders.Add(destinationFolder);
        }

        public void Delete() {}

        public void MoveToFolder(string destinationFolder)
        {
            MoveToFolders.Add(destinationFolder);
        }

        public IEnumerable<IIncomingEmailAttachment> Attachments { get; set; }

        public readonly List<string> MoveToFolders = new List<string>();
        public readonly List<string> CopyToFolders = new List<string>();

        public Exception ExceptionToThrow { get; set; }

        #region Random Generation

        private static readonly Random Rand;
        private static int _seed;

        private const string EmailSuffix = "@blah.com";

        private static IEnumerable<string> GetRandomAliasList(int numAliases)
        {
            if (numAliases == 0)
            {
                return new List<string>();
            }

            var aliases = new List<string>(numAliases);
            for (var i = 0; i < numAliases; ++i )
            {
                aliases.Add(RandomDataHelper.GetAlias(_seed++) + EmailSuffix);
            }

            return aliases;
        }

        private static IEnumerable<string> GetRandomNamesList(int numNames)
        {
            if (numNames == 0)
            {
                return new List<string>();
            }

            var names = new List<string>(numNames);
            for (var i = 0; i < numNames; ++i)
            {
                names.Add(RandomDataHelper.GetName(_seed++));
            }

            return names;
        }

        #endregion

        private static readonly ILog Logger = LogManager.GetLogger(typeof (IncomingEmailMessageMock));
    }
}
