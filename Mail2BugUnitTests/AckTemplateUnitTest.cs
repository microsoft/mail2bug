using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using Mail2BugUnitTests.Mocks.Email;
using Mail2Bug;
using log4net;
using System.Linq;
using Mail2Bug.WorkItemManagement;
using System.Collections.Generic;
using Mail2Bug.Email;

namespace Mail2BugUnitTests
{
    [TestClass]
    public class AckTemplateUnitTest
    {
        [TestMethod]
        public void TestPlaceholder()
        {
            var rand = new Random();
            var seed = rand.Next();

            Logger.InfoFormat("Using seed {0}", seed);

            var instanceConfig = GetConfig().Instances.First();
            var templateText = File.ReadAllText(@"Templates\AckTemplate.htm");
            var placeholders = AckEmailPlaceholder.GetPlaceholders(templateText);
            var fields = GetWorkItemFields(seed);
            var workItem = new WorkItemFieldsMock(fields);

            var template = new AckEmailTemplate(templateText);
            var body = template.Apply(workItem, instanceConfig);

            Assert.IsTrue(body.Contains(workItem.ID), "Work item ID has not been replaced.");
            Assert.IsTrue(body.Contains(workItem.Title), "Work item title has not been replaced.");
            Assert.IsTrue(body.Contains(workItem.State), "Work item state has not been replaced.");
            Assert.IsTrue(body.Contains(workItem.Owner), "Work item owner has not been replaced.");

            foreach (var placeholder in placeholders)
            {
                Assert.IsFalse(body.Contains(placeholder.Text), $"Placeholder {placeholder.Text} has not been replaced.");
            }
        }

        [TestMethod]
        public void TestPlaceholderGlobals()
        {
            var rand = new Random();
            var seed = rand.Next();

            Logger.InfoFormat("Using seed {0}", seed);

            var instanceConfig = GetConfig().Instances.First();
            var templateText = File.ReadAllText(@"Templates\AckTemplate.htm");
            var fields = GetWorkItemFields(seed);
            var workItem = new WorkItemFieldsMock(fields);

            var template = new AckEmailTemplate(templateText);
            var body = template.Apply(workItem, instanceConfig);

            Assert.IsTrue(body.Contains(instanceConfig.TfsServerConfig.CollectionUri), "TFS CollectionURI has not been replaced.");
            Assert.IsTrue(body.Contains(instanceConfig.TfsServerConfig.Project), "TFS Project has not been replaced.");
            Assert.IsTrue(body.Contains(instanceConfig.TfsServerConfig.WorkItemTemplate), "Work item template has not been replaced.");
            Assert.IsTrue(body.Contains(instanceConfig.EmailSettings.Recipients), "Mail2Bug recipients has not been replaced.");
        }

        [TestMethod]
        public void TestPlaceholderInvalid()
        {
            var rand = new Random();
            var seed = rand.Next();

            Logger.InfoFormat("Using seed {0}", seed);

            var instanceConfig = GetConfig().Instances.First();
            var templateText = File.ReadAllText(@"Templates\AckTemplate.htm");
            var placeholders = AckEmailPlaceholder.GetPlaceholders(templateText);
            var fields = GetWorkItemFields(seed);
            var workItem = new WorkItemFieldsMock(fields);

            var namedField = "Priority";
            var namedPlaceholder = placeholders.First(p => p.Field.Equals(namedField, StringComparison.InvariantCultureIgnoreCase));
            Assert.IsNotNull(namedPlaceholder, $"Bad test data; placeholder for {namedField} not found in template.");

            var template = new AckEmailTemplate(templateText);
            var body = template.Apply(workItem, instanceConfig);

            Assert.IsFalse(body.Contains(namedPlaceholder.Text), $"Invalid placeholder {namedPlaceholder.Text} has not been removed.");
        }

        [TestMethod]
        public void TestPlaceholderForNamedField()
        {
            var rand = new Random();
            var seed = rand.Next();

            Logger.InfoFormat("Using seed {0}", seed);

            var instanceConfig = GetConfig().Instances.First();
            var templateText = File.ReadAllText(@"Templates\AckTemplate.htm");
            var placeholders = AckEmailPlaceholder.GetPlaceholders(templateText);
            var fields = GetWorkItemFields(seed);

            var namedField = "Priority";
            var namedFieldValue = "PRI2";
            fields[namedField] = namedFieldValue;
            var namedPlaceholder = placeholders.First(p => p.Field.Equals(namedField, StringComparison.InvariantCultureIgnoreCase));
            Assert.IsNotNull(namedPlaceholder, $"Bad test data; placeholder for {namedField} not found in template.");
            var workItem = new WorkItemFieldsMock(fields);

            var template = new AckEmailTemplate(templateText);
            var body = template.Apply(workItem, instanceConfig);

            Assert.IsFalse(body.Contains(namedPlaceholder.Text), $"Placeholder {namedPlaceholder.Text} has not been replaced.");
            Assert.IsTrue(body.Contains(namedFieldValue), $"Placeholder {namedPlaceholder.Text} has not been replaced with {namedFieldValue}.");
        }

        [TestMethod]
        public void TestPlaceholderWithDefault()
        {
            var rand = new Random();
            var seed = rand.Next();

            Logger.InfoFormat("Using seed {0}", seed);

            var instanceConfig = GetConfig().Instances.First();
            var templateText = File.ReadAllText(@"Templates\AckTemplate.htm");
            var placeholders = AckEmailPlaceholder.GetPlaceholders(templateText);
            var fields = GetWorkItemFields(seed);

            fields["Assigned To"] = null;
            fields["Priority"] = null;
            var placeholdersWithDefaults = placeholders.Where(p => p.Field.Equals("BugOwner") || p.Field.Equals("Priority")).ToList();
            Assert.AreEqual(2, placeholdersWithDefaults.Count(), "Bad test data; all expected placeholders not found.");

            var workItem = new WorkItemFieldsMock(fields);
            var template = new AckEmailTemplate(templateText);
            var body = template.Apply(workItem, instanceConfig);

            foreach (var placeholder in placeholdersWithDefaults)
            {
                Assert.IsFalse(body.Contains(placeholder.Text), $"Placeholder '{placeholder.Text}' has not been replaced.");
                Assert.IsTrue(body.Contains(placeholder.DefaultValue), $"Placeholder '{placeholder.Text}' has not been replaced with its default value '{placeholder.DefaultValue}'.");
            }
        }
        
        private static Config GetConfig()
        {
            return Config.GetConfig("AckTemplateUnitTestConfig.xml");
        }

        private static Dictionary<string, string> GetWorkItemFields(int workItemId)
        {
            var fieldValues = new Dictionary<string, string> {
                { "ID", workItemId.ToString() },
                { "Title", $"Work item {workItemId}" },
                { "Assigned To", $"Owner {workItemId}" },
                { "State", "New" }
            };

            return fieldValues;
        }

        readonly Random _rand = new Random();
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SimpleBugStrategyUnitTest));
    }
}
