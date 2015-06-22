using System;
using System.Collections.Generic;
using System.Linq;
using Mail2Bug.Email;
using Mail2Bug.Email.EWS;
using Mail2BugUnitTests.Mocks.Email;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Mail2BugUnitTests
{
    [TestClass]
    public class RecipientsMailboxManagerRouterUnitTest
    {
        IIncomingEmailMessage[] CreateMessages(
            int numMessages,
            string subjectBase,
            DateTime startTime,
            TimeSpan intervalBetweenMessages)
        {
            var result = new IIncomingEmailMessage[numMessages];
            var receivedTime = startTime;
            for (var i = 0; i < numMessages; ++i, receivedTime += intervalBetweenMessages)
            {
                var message = new IncomingEmailMessageMock
                {
                    Subject = string.Format("{0}_{1}", subjectBase, i),
                    ReceivedOn = receivedTime
                };
                result[i] = message;
            }

            return result;
        }

        void AssertMessageListsAreEqual(IEnumerable<IIncomingEmailMessage> expected, IEnumerable<IIncomingEmailMessage> actual)
        {
            // The expectation is for results to be returned sorted by received time (from earliest to latest), so we sort the
            // expected messages based on ReceivedOn in ascending order
            var expectedMessages = (expected.OrderBy(m => m.ReceivedOn)).ToArray();

            var actualMessages = actual as IIncomingEmailMessage[] ?? actual.ToArray();

            Assert.AreEqual(expectedMessages.Length, actualMessages.Length);
            for (var i = 0; i < expectedMessages.Length; ++i)
            {
                Assert.AreEqual(expectedMessages[i].Subject, actualMessages[i].Subject);
            }
        }
            
        // Basic test: Construction, and a single mailbox registered. When calling GetMessages for
        // the first time, we get the relevant messages.
        [TestMethod]
        public void SingleMailboxBasicTest()
        {
            // Populate two messages, then register a client that accepts all messages
            // The expectation is that when we call GetMessages, we'll get exactly the messages we populated
            // for the input
            var inputMessages = CreateMessages(2, "Subject", DateTime.Now.AddDays(-1), TimeSpan.FromSeconds(1));
            var mock = new Mock<IMailFolder>();
            mock.Setup(x => x.GetMessages()).Returns(inputMessages);

            var router = new RecipientsMailboxManagerRouter(mock.Object);
            var id = router.RegisterMailbox(m => true);
            router.ProcessInbox();
            
            // Retrieve
            var output = router.GetMessages(id).ToArray();

            // Validate that it's the exact same set that we populated
            AssertMessageListsAreEqual(inputMessages, output);
        }

        // Test that when new messages arrive, calling GetMessages retrieves them
        [TestMethod]
        public void SingleMailboxDetectNewMessagesTest()
        {
            // Populate some messages and retrieve them
            var inputMessages = CreateMessages(2, "Subject", DateTime.Now.AddDays(-1), TimeSpan.FromSeconds(1));
            var mock = new Mock<IMailFolder>();
            mock.Setup(x => x.GetMessages()).Returns(inputMessages);

            var router = new RecipientsMailboxManagerRouter(mock.Object);
            var id = router.RegisterMailbox(m => true);
            router.ProcessInbox();
            
            var output = router.GetMessages(id).ToArray();
            AssertMessageListsAreEqual(inputMessages, output);

            // Now populate a new message that is newer than the one we handled before, then call GetMessages again
            // and make sure it is retrieved
            var newMessages = CreateMessages(1, "Subject", DateTime.Now, TimeSpan.Zero);
            mock.Setup(x => x.GetMessages()).Returns(newMessages);
            router.ProcessInbox();

            output = router.GetMessages(id).ToArray();
            AssertMessageListsAreEqual(newMessages, output);
        }

        [TestMethod]
        public void SingleMailboxWIthIgnoredMessagesTest()
        {
            // Populate some messages 
            var inputMessages = CreateMessages(10, "Subject", DateTime.Now.AddDays(-1), TimeSpan.FromSeconds(1));
            var mock = new Mock<IMailFolder>();
            mock.Setup(x => x.GetMessages()).Returns(inputMessages);

            // Only one of the messages is relevant for the client
            var relevantMessages = new List<IIncomingEmailMessage> { inputMessages[3] };

            var router = new RecipientsMailboxManagerRouter(mock.Object);
            var id = router.RegisterMailbox(m => relevantMessages.Any(message => message.Subject == m.Subject));
            router.ProcessInbox();

            var output = router.GetMessages(id).ToArray();
            AssertMessageListsAreEqual(relevantMessages, output);
        }

        // Test that when registering two clients with disjoint recipients, each gets its own messages
        [TestMethod]
        public void TwoMailboxesBasicTest()
        {
            // Populate some messages and retrieve them
            var inputMessages = CreateMessages(100, "Subject", DateTime.Now.AddDays(-1), TimeSpan.FromSeconds(1));
            var mock = new Mock<IMailFolder>();
            mock.Setup(x => x.GetMessages()).Returns(inputMessages);

            // Set the timeout threshold to 0, so that we always re-process the messages in the folder, even if there
            // are no new items
            var router = new RecipientsMailboxManagerRouter(mock.Object);
            var client1Messages = new List<IIncomingEmailMessage>{inputMessages[1], inputMessages[4]};
            var client2Messages = new List<IIncomingEmailMessage>{inputMessages[8], inputMessages[7]};
            
            var client1Id = router.RegisterMailbox( 
                m => client1Messages.Any(message => message.Subject == m.Subject) );
            
            var client2Id = router.RegisterMailbox(
                m => client2Messages.Any(message => message.Subject == m.Subject) );

            router.ProcessInbox();

            var client1Output = router.GetMessages(client1Id).ToArray();
            var client2Output = router.GetMessages(client2Id).ToArray();
            AssertMessageListsAreEqual(client1Messages, client1Output);
            AssertMessageListsAreEqual(client2Messages, client2Output);
        }

        // Test that when registering two client with overlapping evaluator predicates, messages that can be
        // routed to either of them are only routed to *one* of them and not both
        [TestMethod]
        public void OverlappingRecipientsTest()
        {
            // Populate some messages and retrieve them
            var inputMessages = CreateMessages(100, "Subject", DateTime.Now.AddDays(-1), TimeSpan.FromSeconds(1));
            var mock = new Mock<IMailFolder>();
            mock.Setup(x => x.GetMessages()).Returns(inputMessages);

            // Set the timeout threshold to 0, so that we always re-process the messages in the folder, even if there
            // are no new items
            var router = new RecipientsMailboxManagerRouter(mock.Object);
            
            var client1UniqueMessage = inputMessages[4];
            var client2UniqueMessage = inputMessages[7];
            var sharedMessage = inputMessages[1];
            
            var client1Messages = new List<IIncomingEmailMessage> { sharedMessage, client1UniqueMessage };
            var client2Messages = new List<IIncomingEmailMessage> { sharedMessage, client2UniqueMessage };
            var client1Id = router.RegisterMailbox(
                m => { return client1Messages.Any(message => message.Subject == m.Subject); });
            var client2Id = router.RegisterMailbox(
                m => { return client2Messages.Any(message => message.Subject == m.Subject); });

            router.ProcessInbox();

            var client1Output = router.GetMessages(client1Id).ToArray();
            var client2Output = router.GetMessages(client2Id).ToArray();

            // Make sure that each client gets the message unique for them
            Assert.AreEqual(1, client1Output.Count(m => m.Subject == client1UniqueMessage.Subject));
            Assert.AreEqual(1, client2Output.Count(m => m.Subject == client2UniqueMessage.Subject));

            // The shared message should only appear once (in either of them)
            Assert.AreEqual(1, client1Output.Concat(client2Output).Count(m => m.Subject == sharedMessage.Subject));

            // There shouldn't be any other messages, so total messages should be 3
            Assert.AreEqual(3, client1Output.Concat(client2Output).Count());
        }

        // Test that registering a lot of mailboxes works and routing behaves as expected (i.e. each
        // client gets only the messages the expect, and messages aren't dropped)
        [TestMethod]
        public void ManyMailboxesTest()
        {
            const int numMailboxes = 100;
            var rand = new Random();

            var mock = new Mock<IMailFolder>();
            var router = new RecipientsMailboxManagerRouter(mock.Object);

            // Each client has a list of messages relevant to it, an ID, and a predicate
            var clients = new List<Tuple<IEnumerable<IIncomingEmailMessage>, int>>(numMailboxes);
            for (int i = 0; i < numMailboxes; i++)
            {
                var clientMessages = 
                    CreateMessages(
                        rand.Next(1, 10), 
                        string.Format("Subject_{0}", i), 
                        DateTime.Now.AddDays(-i), 
                        TimeSpan.FromSeconds(1));

                var id = router.RegisterMailbox(m => clientMessages.Any(message => message.Subject == m.Subject));

                clients.Add(new Tuple<IEnumerable<IIncomingEmailMessage>, int>(
                    clientMessages, 
                    id));
            }

            var messages = new List<IIncomingEmailMessage>();
            foreach (var client in clients)
            {
                messages.AddRange(client.Item1);
            }
            messages.AddRange(CreateMessages(rand.Next(1,100), "Dummy", DateTime.Now.AddDays(-3),TimeSpan.FromHours(3)));

            mock.Setup(x => x.GetMessages()).Returns(messages);

            router.ProcessInbox();

            foreach (var client in clients)
            {
                var output = router.GetMessages(client.Item2);
                AssertMessageListsAreEqual(client.Item1, output);
            }
        }
    }
}
