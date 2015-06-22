using System;
using System.Collections.Generic;
using System.Text;
using Mail2Bug.Email;
using Mail2Bug.TestHelpers;

namespace Mail2BugUnitTests.Mocks.Email
{
    public class MailManagerMock : IMailboxManager
    {
        private readonly List<IIncomingEmailMessage> _messages = new List<IIncomingEmailMessage>();
        private readonly List<IIncomingEmailMessage> _postProcessedMessages = new List<IIncomingEmailMessage>(); 

        public void Clear()
        {
            _messages.Clear();
            _postProcessedMessages.Clear();
        }

        public IncomingEmailMessageMock AddReply(IIncomingEmailMessage message, string replyText)
        {
            // Reply body contains the reply text, then a message separator, then the previous message's full text
            var bodyBuilder = new StringBuilder(replyText);
            bodyBuilder.AppendLine(RandomDataHelper.GetRandomMessageSeparator(_rand.Next()));
            bodyBuilder.AppendLine(message.PlainTextBody);
            
            var newMessage = AddMessage("RE: " + message.Subject, bodyBuilder.ToString());
            newMessage.SentOn = message.SentOn.AddSeconds(1);
            newMessage.ConversationIndex = GenerateReplyIndex(message.ConversationIndex);

            return newMessage;
        }

        private string GenerateReplyIndex(string conversationIndex)
        {
            var sb = new StringBuilder(conversationIndex);
            sb.Append('0' + _rand.Next(0, 9));
            sb.Append('0' + _rand.Next(0, 9));
            sb.Append('0' + _rand.Next(0, 9));
            sb.Append('0' + _rand.Next(0, 9));
            
            return sb.ToString();
        }

        public IncomingEmailMessageMock AddMessage(string subject, string body)
        {
            var message = AddMessage(false);
            message.Subject = subject;
            message.PlainTextBody = body;
            message.RawBody = body;

            return message;
        }
 
        public IncomingEmailMessageMock AddMessage(bool withAttachments)
        {
            var message = IncomingEmailMessageMock.CreateWithRandomData(withAttachments);
            _messages.Add(message);
            return message;
        }

        public IEnumerable<IIncomingEmailMessage> ReadMessages()
        {
            return _messages;
        }

        public void OnProcessingFinished(IIncomingEmailMessage message, bool successful)
        {
            _postProcessedMessages.Add(message);
        }

        public IEnumerable<IIncomingEmailMessage> PostProcessedMessages { get { return _postProcessedMessages; }}

        readonly Random _rand = new Random();
    }
}
