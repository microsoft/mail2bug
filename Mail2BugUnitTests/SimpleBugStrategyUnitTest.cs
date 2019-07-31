using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using Mail2Bug;
using Mail2Bug.Helpers;
using Mail2Bug.MessageProcessingStrategies;
using Mail2Bug.TestHelpers;
using Mail2Bug.WorkItemManagement;
using Mail2BugUnitTests.Mocks.Email;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mail2BugUnitTests
{
    [TestClass]
    public class SimpleBugStrategyUnitTest
    {
        [TestMethod]
        public void TestProcessingOneMessage()
        {
            var rand = new Random();
            var seed = rand.Next();

            Logger.InfoFormat("Using seed {0}", seed);

            var mailManager = new MailManagerMock();
            mailManager.AddMessage(false);

            var instanceConfig = GetConfig().Instances.First();
            var workItemManagerMock = new WorkItemManagerMock(instanceConfig.WorkItemSettings.ConversationIndexFieldName);
            
            ProcessMailbox(mailManager, instanceConfig, workItemManagerMock);

            Assert.AreEqual(1, workItemManagerMock.Bugs.Count, "Only one bug should exist");
            var bug = workItemManagerMock.Bugs.First();
            foreach (var defaultValue in instanceConfig.WorkItemSettings.DefaultFieldValues)
            {
                var fieldValues = bug.Value;
                var fieldName = defaultValue.Field;
                Assert.IsTrue(fieldValues.ContainsKey(fieldName),
                    string.Format("Default value {0} isn't set in the bug", fieldName));
                Assert.AreEqual(defaultValue.Value, fieldValues[fieldName],
                    string.Format("Value of field {0} is different than expected", fieldName));
            }
        }

        [TestMethod]
        public void TestSpecialValues()
        {
            var rand = new Random();
            var seed = rand.Next();

            Logger.InfoFormat("Using seed {0}", seed);

            var mailManager = new MailManagerMock();
            var message = mailManager.AddMessage(false);

            var instanceConfig = GetConfig().Instances.First();
            
            // Remove the standard default values and replace them with a few fields that are assigned using special values
            instanceConfig.WorkItemSettings.DefaultFieldValues.Clear();
            
            const string nowField = "Now";
            const string todayField = "Today";
            const string messageBodyField = "MB";
            const string messageBodyWithSenderField = "MBWS";
            const string senderField = "Sender";
            const string subjectField = "Subject";
            instanceConfig.WorkItemSettings.DefaultFieldValues.Add(new Config.DefaultValueDefinition { Field = nowField, Value = SpecialValueResolver.NowKeyword });
            instanceConfig.WorkItemSettings.DefaultFieldValues.Add(new Config.DefaultValueDefinition { Field = todayField, Value = SpecialValueResolver.TodayKeyword });
            instanceConfig.WorkItemSettings.DefaultFieldValues.Add(new Config.DefaultValueDefinition { Field = messageBodyField, Value = SpecialValueResolver.MessageBodyKeyword });
            instanceConfig.WorkItemSettings.DefaultFieldValues.Add(new Config.DefaultValueDefinition { Field = messageBodyWithSenderField, Value = SpecialValueResolver.MessageBodyWithSenderKeyword });
            instanceConfig.WorkItemSettings.DefaultFieldValues.Add(new Config.DefaultValueDefinition { Field = subjectField, Value = SpecialValueResolver.SubjectKeyword });
            instanceConfig.WorkItemSettings.DefaultFieldValues.Add(new Config.DefaultValueDefinition { Field = senderField, Value = SpecialValueResolver.SenderKeyword });


            var workItemManagerMock = new WorkItemManagerMock(
                instanceConfig.WorkItemSettings.ConversationIndexFieldName, new IdentityFunctionNameResolverMock());
            ProcessMailbox(mailManager, instanceConfig, workItemManagerMock);

            Assert.AreEqual(1, workItemManagerMock.Bugs.Count, "Only one bug should exist");
            var bugValues = workItemManagerMock.Bugs.First().Value;

            ValidateBugValue(bugValues, nowField, DateTime.Now.ToString("g"));
            ValidateBugValue(bugValues, todayField, DateTime.Now.ToString("d"));
            ValidateBugValue(bugValues, messageBodyField, message.PlainTextBody);
            ValidateBugValue(bugValues, messageBodyWithSenderField, String.Format("{0}\n\nCreated by: {1} ({2})", message.PlainTextBody, message.SenderName, message.SenderAddress));
            ValidateBugValue(bugValues, senderField, message.SenderName);
            ValidateBugValue(bugValues, subjectField, message.ConversationTopic);
        }

        private static void ValidateBugValue(Dictionary<string, string> bugValues, string fieldName, string expectedValue)
        {
            Assert.IsTrue(bugValues.ContainsKey(fieldName), string.Format("Default value {0} isn't set in the bug", fieldName));
            Assert.AreEqual(expectedValue, bugValues[fieldName], string.Format("Value of field {0} is different than expected", fieldName));
        }

        [TestMethod]
        public void TestMnemonics()
        {
            var seed = _rand.Next();

            var mnemonicDef = new Config.MnemonicDefinition
            {Mnemonic = "UPPERCASElowercase", Field = "Mnemonic Field", Value = "Mnemonic Value"};

            Logger.InfoFormat("Using seed {0}", seed);

            var mailManager = new MailManagerMock();
            var message = mailManager.AddMessage(false);
            message.PlainTextBody += string.Format("\n@@@{0}\n", mnemonicDef.Mnemonic.ToLower());

            var instanceConfig = GetConfig().Instances.First();
            instanceConfig.WorkItemSettings.Mnemonics.Add(mnemonicDef);
            var workItemManagerMock = new WorkItemManagerMock(instanceConfig.WorkItemSettings.ConversationIndexFieldName);
            ProcessMailbox(mailManager, instanceConfig, workItemManagerMock);

            Assert.AreEqual(1, workItemManagerMock.Bugs.Count, "Only one bug should exist");
            var bug = workItemManagerMock.Bugs.First();
            foreach (var defaultValue in instanceConfig.WorkItemSettings.DefaultFieldValues)
            {
                var fieldValues = bug.Value;
                var fieldName = defaultValue.Field;
                Assert.IsTrue(fieldValues.ContainsKey(fieldName), string.Format("Default value {0} isn't set in the bug", fieldName));
                Assert.AreEqual(defaultValue.Value, fieldValues[fieldName], string.Format("Value of field {0} is different than expected", fieldName));
            }

            Assert.IsTrue(bug.Value.ContainsKey(mnemonicDef.Field), "Check mnemonic field is set");
            Assert.AreEqual(mnemonicDef.Value, bug.Value[mnemonicDef.Field],
                "Check mnemonic field contains the right value");
        }

        [TestMethod]
        public void TestExplicitOverrides()
        {
            var seed = _rand.Next();

            Logger.InfoFormat("Using seed {0}", seed);

            const string explicitField = "Explicitly Overridden Field";
            const string explicitValue = "Explicitly Overridden Value";

            var mailManager = new MailManagerMock();
            var message = mailManager.AddMessage(false);
            message.PlainTextBody += string.Format("\n###{0} : {1}  \n", explicitField, explicitValue);

            var instanceConfig = GetConfig().Instances.First();
            var workItemManagerMock = new WorkItemManagerMock(instanceConfig.WorkItemSettings.ConversationIndexFieldName);
            ProcessMailbox(mailManager, instanceConfig, workItemManagerMock);

            Assert.AreEqual(1, workItemManagerMock.Bugs.Count, "Only one bug should exist");
            var bug = workItemManagerMock.Bugs.First();
            foreach (var defaultValue in instanceConfig.WorkItemSettings.DefaultFieldValues)
            {
                var fieldValues = bug.Value;
                var fieldName = defaultValue.Field;
                Assert.IsTrue(fieldValues.ContainsKey(fieldName), string.Format("Default value {0} isn't set in the bug", fieldName));
                Assert.AreEqual(defaultValue.Value, fieldValues[fieldName], string.Format("Value of field {0} is different than expected", fieldName));
            }

            Assert.IsTrue(bug.Value.ContainsKey(explicitField), "Check explicitly overriden field is set");
            Assert.AreEqual(explicitValue, bug.Value[explicitField], "Check explicitly overriden FIELD contains the right value");
        }

        [TestMethod]
        public void TestProcessingEmailThreadOverrideChangedBy()
        {
            TestProcessingEmailThreadImpl(true);
        }

        [TestMethod]
        public void TestProcessingEmailThreadDontOverrideChangedBy()
        {
            TestProcessingEmailThreadImpl(false);
        }

        public void TestProcessingEmailThreadImpl(bool overrideChangedBy)
        {
            var seed = _rand.Next();

            Logger.InfoFormat("Using seed {0}", seed);

            var mailManager = new MailManagerMock();
            var message1 = mailManager.AddMessage(false);
            var message2 = mailManager.AddReply(message1, RandomDataHelper.GetBody(seed));
            var message3 = mailManager.AddReply(message2, RandomDataHelper.GetBody(seed));

            var instanceConfig = GetConfig().Instances.First();
            instanceConfig.WorkItemSettings.OverrideChangedBy = overrideChangedBy;

            var workItemManagerMock = new WorkItemManagerMock(instanceConfig.WorkItemSettings.ConversationIndexFieldName);
            ProcessMailbox(mailManager, instanceConfig, workItemManagerMock);

            Assert.AreEqual(1, workItemManagerMock.Bugs.Count, "Only one bug should exist");
            var bug = workItemManagerMock.Bugs.First();
            var bugFields = bug.Value;

            var expectedValues = new Dictionary<string,string>();
            instanceConfig.WorkItemSettings.DefaultFieldValues.ForEach(x=> expectedValues[x.Field] = x.Value);

            if (overrideChangedBy)
            {
                expectedValues["Changed By"] = message3.SenderName;
            }
            expectedValues[WorkItemManagerMock.HistoryField] = TextUtils.FixLineBreaks(message2.GetLastMessageText(true) + message3.GetLastMessageText(true));

            ValidateBugValues(expectedValues, bugFields);
        }

        [TestMethod]
        public void TestApplyingOverridesInUpdateMessage()
        {
            var seed = _rand.Next();

            Logger.InfoFormat("Using seed {0}", seed);

            var mailManager = new MailManagerMock();
            var message1 = mailManager.AddMessage(false);

            var mnemonicDef = new Config.MnemonicDefinition { Mnemonic = "myMnemonic", Field = "Mnemonic Field", Value = "Mnemonic Value" };
            var mnemonicLine = string.Format("\n@@@{0}\n", mnemonicDef.Mnemonic);

            var explicitOverride1 = new KeyValuePair<string, string>("IsThisExplicit?","Indeed");
            var explicitLine1 = string.Format("\n###{0}:{1}\n", explicitOverride1.Key, explicitOverride1.Value);

            var explicitOverride2 = new KeyValuePair<string, string>("WillThisOneBeProcessed?","No");
            var explicitLine2 = string.Format("\n###{0}:{1}\n", explicitOverride2.Key, explicitOverride2.Value);

            var message2 = mailManager.AddReply(message1, mnemonicLine + RandomDataHelper.GetBody(seed));
            var message3 = mailManager.AddReply(message2, RandomDataHelper.GetBody(seed) + explicitLine1);

            // This last override will not be considered, because it's not part of the last message (it's at the
            // end of the message text, so considered as part of the first message)
            var message4 = mailManager.AddReply(message3, RandomDataHelper.GetBody(seed));
            message4.PlainTextBody += explicitLine2;

            var instanceConfig = GetConfig().Instances.First();
            instanceConfig.WorkItemSettings.ApplyOverridesDuringUpdate = true;
            instanceConfig.WorkItemSettings.Mnemonics.Add(mnemonicDef);

            var workItemManagerMock = new WorkItemManagerMock(instanceConfig.WorkItemSettings.ConversationIndexFieldName);
            ProcessMailbox(mailManager, instanceConfig, workItemManagerMock);

            Assert.AreEqual(1, workItemManagerMock.Bugs.Count, "Only one bug should exist");
            var bug = workItemManagerMock.Bugs.First();
            var bugFields = bug.Value;

            var expectedValues = new Dictionary<string,string>();
            instanceConfig.WorkItemSettings.DefaultFieldValues.ForEach(x=> expectedValues[x.Field] = x.Value);

            expectedValues["Changed By"] = message4.SenderName;
            expectedValues[WorkItemManagerMock.HistoryField] = 
                TextUtils.FixLineBreaks(message2.GetLastMessageText(true) + message3.GetLastMessageText(true) + message4.GetLastMessageText(true));
            expectedValues[mnemonicDef.Field] = mnemonicDef.Value;
            expectedValues[explicitOverride1.Key] = explicitOverride1.Value;

            ValidateBugValues(expectedValues, bugFields);
            Assert.IsFalse(bugFields.ContainsKey(explicitOverride2.Key));
        }

        [TestMethod]
        public void TestApplyingOverridesInUpdateMessage_Html()
        {
            var seed = _rand.Next();

            Logger.InfoFormat("Using seed {0}", seed);

            var mailManager = new MailManagerMock();
            var explicitOverride1 = new KeyValuePair<string, string>("IsThisExplicit?","Indeed");
            var explicitLine1 = string.Format("\n###{0}:{1}", explicitOverride1.Key, explicitOverride1.Value);
            var message1 = mailManager.AddMessage(false);

            var htmlText = string.Format("<html><head></head><body><p>{0}</p></body></html>", explicitLine1);
            var message2 = mailManager.AddReply(message1, htmlText);
            message2.IsHtmlBody = true;
            message2.PlainTextBody = explicitLine1;

            var instanceConfig = GetConfig().Instances.First();
            instanceConfig.WorkItemSettings.ApplyOverridesDuringUpdate = true;
            instanceConfig.WorkItemSettings.EnableExperimentalHtmlFeatures = true;

            var workItemManagerMock = new WorkItemManagerMock(instanceConfig.WorkItemSettings.ConversationIndexFieldName);
            ProcessMailbox(mailManager, instanceConfig, workItemManagerMock);

            Assert.AreEqual(1, workItemManagerMock.Bugs.Count, "Only one bug should exist");
            var bug = workItemManagerMock.Bugs.First();
            var bugFields = bug.Value;

            Assert.IsTrue(bugFields.ContainsKey(explicitOverride1.Key));
            string actualValue = bugFields[explicitOverride1.Key];
            Assert.AreEqual(explicitOverride1.Value, actualValue);
        }

        [TestMethod]
        public void TestAttachingUpdateMessages()
        {
            var seed = _rand.Next();

            Logger.InfoFormat("Using seed {0}", seed);

            var mailManager = new MailManagerMock();
            var message1 = mailManager.AddMessage(false);
            var message2 = mailManager.AddReply(message1, "message 1");
            var message3 = mailManager.AddReply(message2, "message 2");

            var instanceConfig = GetConfig().Instances.First();
            instanceConfig.WorkItemSettings.AttachUpdateMessages = true;

            var workItemManagerMock = new WorkItemManagerMock(instanceConfig.WorkItemSettings.ConversationIndexFieldName);
            ProcessMailbox(mailManager, instanceConfig, workItemManagerMock);

            Assert.AreEqual(1, workItemManagerMock.Bugs.Count, "Only one bug should exist");
            var bug = workItemManagerMock.Bugs.First();
            var bugFields = bug.Value;

            var expectedValues = new Dictionary<string,string>();
            instanceConfig.WorkItemSettings.DefaultFieldValues.ForEach(x=> expectedValues[x.Field] = x.Value);

            expectedValues["Changed By"] = message3.SenderName;
            expectedValues[WorkItemManagerMock.HistoryField] = 
                TextUtils.FixLineBreaks(message2.GetLastMessageText(true) + message3.GetLastMessageText(true));

            ValidateBugValues(expectedValues, bugFields);

            Assert.IsTrue(workItemManagerMock.Attachments.ContainsKey(bug.Key));
            Assert.AreEqual(workItemManagerMock.Attachments[bug.Key].Count, 3);
        }

        [TestMethod]
        public void TestAppendOnlyMessageBody()
        {
            var seed = _rand.Next();

            Logger.InfoFormat("Using seed {0}", seed);

            var mailManager = new MailManagerMock();
            var message1 = mailManager.AddMessage(false);

            var instanceConfig = GetConfig().Instances.First();

            var workItemManagerMock = new WorkItemManagerMock(instanceConfig.WorkItemSettings.ConversationIndexFieldName);
            ProcessMailbox(mailManager, instanceConfig, workItemManagerMock);
            Assert.AreEqual(1, workItemManagerMock.Bugs.Count, "Only one bug should exist");

            var newBugId = workItemManagerMock.Bugs.First().Key + 1;
            workItemManagerMock.Bugs.Add(newBugId, new Dictionary<string, string>());

            mailManager.Clear();
                
            var appendOnlyMessage = mailManager.AddMessage(false);
            appendOnlyMessage.PlainTextBody = string.Format("Blah !!!bug #{0}freqmnclkwerqcnew", newBugId);

            // Message has a conversation index that suggests it's related to a thread with some work item ID x, but it has 
            // a subject that connects it to work item ID x+1
            // Should end up applying to the latter work-item (x+1)
            appendOnlyMessage.ConversationId = message1.ConversationId + "AAAA";

            ProcessMailbox(mailManager, instanceConfig, workItemManagerMock);

            var expectedValues = new Dictionary<string, string>();
            expectedValues["Changed By"] = appendOnlyMessage.SenderName;
            expectedValues[WorkItemManagerMock.HistoryField] = TextUtils.FixLineBreaks(appendOnlyMessage.GetLastMessageText(false));

            Assert.AreEqual(2, workItemManagerMock.Bugs.Count, "Only one bug should exist");
            ValidateBugValues(expectedValues, workItemManagerMock.Bugs[newBugId]);
        }

        [TestMethod]
        public void TestAttachOriginalMessage()
        {
            var rand = new Random();
            var seed = rand.Next();

            Logger.InfoFormat("Using seed {0}", seed);

            var mailManager = new MailManagerMock();
            mailManager.AddMessage(false);

            var instanceConfig = GetConfig().Instances.First();
            instanceConfig.WorkItemSettings.AttachOriginalMessage = true;
            var workItemManagerMock = new WorkItemManagerMock(instanceConfig.WorkItemSettings.ConversationIndexFieldName);
            ProcessMailbox(mailManager, instanceConfig, workItemManagerMock);

            Assert.AreEqual(1, workItemManagerMock.Bugs.Count, "Only one bug should exist");
            var bug = workItemManagerMock.Bugs.First();

            Assert.IsTrue(workItemManagerMock.Attachments.ContainsKey(bug.Key));
            Assert.AreEqual(workItemManagerMock.Attachments[bug.Key].Count, 1);
        }

        [TestMethod]
        public void TestAppendOnlyMessageSubject()
        {
            var seed = _rand.Next();

            Logger.InfoFormat("Using seed {0}", seed);

            var mailManager = new MailManagerMock();
            var message1 = mailManager.AddMessage(false);

            var instanceConfig = GetConfig().Instances.First();

            var workItemManagerMock = new WorkItemManagerMock(instanceConfig.WorkItemSettings.ConversationIndexFieldName);
            ProcessMailbox(mailManager, instanceConfig, workItemManagerMock);
            Assert.AreEqual(1, workItemManagerMock.Bugs.Count, "Only one bug should exist");

            var newBugId = workItemManagerMock.Bugs.First().Key + 1;
            workItemManagerMock.Bugs.Add(newBugId, new Dictionary<string, string>());

            mailManager.Clear();
                
            const string comment = "Comment";
            var appendOnlySubject = string.Format("RE: Bug #{0}: coverage drop and fluctuation in CO3 [was: RE: CX LATAM coverage drop observed on 12/29-12/30 UTC (TFS# 719845)]", newBugId);
            var appendOnlyMessage = mailManager.AddMessage(appendOnlySubject, comment);

            // Message has a conversation index that suggests it's related to a thread with some work item ID x, but it has 
            // a subject that connects it to work item ID x+1
            // Should end up applying to the latter work-item (x+1)
            appendOnlyMessage.ConversationId = message1.ConversationId + "AAAA";

            ProcessMailbox(mailManager, instanceConfig, workItemManagerMock);

            var expectedValues = new Dictionary<string, string>();
            expectedValues["Changed By"] = appendOnlyMessage.SenderName;
            expectedValues[WorkItemManagerMock.HistoryField] = TextUtils.FixLineBreaks(appendOnlyMessage.GetLastMessageText(true));

            Assert.AreEqual(2, workItemManagerMock.Bugs.Count, "Only one bug should exist");
            ValidateBugValues(expectedValues, workItemManagerMock.Bugs[newBugId]);
        }

        private static void ValidateBugValues(Dictionary<string, string> expectedValues, Dictionary<string, string> bugFields)
        {
            foreach (var expectedValue in expectedValues)
            {
                var fieldName = expectedValue.Key;
                Assert.IsTrue(bugFields.ContainsKey(fieldName),
                              string.Format("Default value {0} isn't set in the bug", fieldName));
                Assert.AreEqual(expectedValue.Value, bugFields[fieldName],
                                string.Format("Value of field {0} is different than expected", fieldName));
            }
        }

        private static void ProcessMailbox(MailManagerMock mailManager, Config.InstanceConfig instanceConfig,
                                           WorkItemManagerMock workItemManagerMock)
        {
            var sbs = new SimpleBugStrategy(instanceConfig, workItemManagerMock);

            foreach (var incomingEmailMessage in mailManager.ReadMessages())
            {
                sbs.ProcessInboxMessage(incomingEmailMessage);
            }
        }

        private static Config GetConfig()
        {
            return Config.GetConfig("SimpleBugStrategyUnitTestConfig.xml");
        }

        readonly Random _rand = new Random();
        private static readonly ILog Logger = LogManager.GetLogger(typeof (SimpleBugStrategyUnitTest));
    }
}
