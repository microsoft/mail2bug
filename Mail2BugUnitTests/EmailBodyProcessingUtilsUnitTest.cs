using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Mail2Bug.Email;
using Mail2Bug.TestHelpers;
using Mail2BugUnitTests.Mocks.Email;
using Microsoft.Test.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mail2BugUnitTests
{
    [TestClass]
    public class EmailBodyProcessingUtilsUnitTest
    {
        [TestMethod]
        public void TestGetLastMessageTextBasic()
        {
            var message = new IncomingEmailMessageMock();

            var lastMessageText = RandomDataHelper.GetBody(_rand.Next());
            var numOfReplies = _rand.Next(0, 100);
            var bodyBuilder = new StringBuilder(lastMessageText);

            for (var i = 0; i < numOfReplies; i++)
            {
                bodyBuilder.AppendLine(RandomDataHelper.GetRandomMessageSeparator(_rand.Next()));
                bodyBuilder.Append(RandomDataHelper.GetBody(_rand.Next()));
            }
            message.PlainTextBody = bodyBuilder.ToString();

            Assert.AreEqual(lastMessageText, EmailBodyProcessingUtils.GetLastMessageText(message), "Verifying extracted last message text correctness");
        }

        [TestMethod]
        public void TestConvertHtmlMessageToPlainTextBasic()
        {
            var properties = new StringProperties();
            properties.MinNumberOfCodePoints = 20;
            RandomDataHelper.Ranges.ForEach(properties.UnicodeRanges.Add);
            properties.UnicodeRanges.Remove(new UnicodeRange('<', '<'));
            properties.UnicodeRanges.Remove(new UnicodeRange('>', '>'));

            // Can't have '<' or '>' chars in the content, since it breaks the HTML processing. This is OK, since real HTML should never have these
            // chracters either (they will be escaped as &lt; and &gt;)
            var expectedText = 
                StringFactory.GenerateRandomString(properties, _rand.Next()).Trim().Replace("<", "").Replace(">", "");
            var htmlText = string.Format("<html><head></head><body><p>{0}</p></body></html>", expectedText);
            var plainText = EmailBodyProcessingUtils.ConvertHtmlMessageToPlainText(htmlText);

            Assert.AreEqual(plainText, expectedText);
        }

        [TestMethod]
        public void TestConvertHtmlMessageToHtmlRegression()
        {
            const string regressionMessagesFolder = "RegressionMessages";
            foreach (var originalFilename in Directory.GetFiles(regressionMessagesFolder, "*.orig"))
            {
                Trace.WriteLine(string.Format("Processing regression file {0}", originalFilename));

                var baseFilename = Path.GetFileNameWithoutExtension(originalFilename);
                var expectedFilename = Path.Combine(regressionMessagesFolder, baseFilename + ".expected");

                var html = File.ReadAllText(originalFilename);
                var expectedText = Normalize(File.ReadAllText(expectedFilename));
                var convertedHtml = Normalize(EmailBodyProcessingUtils.ConvertHtmlMessageToPlainText(html));

                Assert.AreEqual(expectedText, convertedHtml);
            }
        }

        private static string Normalize(string text)
        {
            var normalized = text.Replace("\r\n", "\n");
            return Regex.Replace(normalized, @"\n\s*\n", "\n\n");
        }

        private readonly Random _rand = new Random();
    }
}
