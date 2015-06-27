using System;
using System.Collections.Generic;
using System.Linq;
using Mail2Bug;
using Mail2Bug.MessageProcessingStrategies;
using Mail2BugUnitTests.Mocks.Email;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mail2BugUnitTests
{
    [TestClass]
    public class OverridesExtractorUnitTest
    {
        [TestMethod]
        public void TestSimpleMnemonicOverride()
        {
            var config = GetConfig();

            config.WorkItemSettings.Mnemonics.Add(
                new Config.MnemonicDefinition { Mnemonic = BasicMnemonic.ToUpper(), Field = BasicField, Value = BasicValue });

            var mailManager = new MailManagerMock();
            var message = mailManager.AddMessage(false);
            message.PlainTextBody += string.Format("@@@{0}", BasicMnemonic.ToLower());

            var extractor = new OverridesExtractor(config);
            var dictionary = extractor.GetOverrides(message);

            Assert.IsTrue(dictionary.ContainsKey(BasicField), string.Format("Validate override for field {0} exists", BasicField));
            Assert.AreEqual(BasicValue, dictionary[BasicField], "Validate the right value was assigned as part of mnemonic resolution");
        }

        [TestMethod]
        public void TestSimpleDateBasedOverride()
        {
            var config = GetConfig();

            config.WorkItemSettings.DateBasedOverrides.Add(new Config.DateBasedFieldOverrides());
            config.WorkItemSettings.DateBasedOverrides[0].FieldName = BasicField;
            config.WorkItemSettings.DateBasedOverrides[0].DefaultValue = BasicValue;
            config.WorkItemSettings.DateBasedOverrides[0].Entries = new List<Config.DateBasedOverrideEntry>
            {
                new Config.DateBasedOverrideEntry {StartDate = DateTime.Now, Value = BasicValue2}
            };

            var mailManager = new MailManagerMock();
            var message = mailManager.AddMessage(false);

            var extractor = new OverridesExtractor(config);
            var dictionary = extractor.GetOverrides(message);

            Assert.IsTrue(dictionary.ContainsKey(BasicField), string.Format("Validate override for field {0} exists", BasicField));
            Assert.AreEqual(BasicValue2, dictionary[BasicField], "Validate the right value was assigned as part of mnemonic resolution");
        }

        [TestMethod]
        public void TestDefaultDateBasedOverride()
        {
            var config = GetConfig();

            config.WorkItemSettings.DateBasedOverrides.Add(new Config.DateBasedFieldOverrides());
            config.WorkItemSettings.DateBasedOverrides[0].FieldName = BasicField;
            config.WorkItemSettings.DateBasedOverrides[0].DefaultValue = BasicValue;
            config.WorkItemSettings.DateBasedOverrides[0].Entries = new List<Config.DateBasedOverrideEntry>
            {
                new Config.DateBasedOverrideEntry
                {
                    StartDate = DateTime.Now + TimeSpan.FromDays(10),
                    Value = BasicValue2
                }
            };

            var mailManager = new MailManagerMock();
            var message = mailManager.AddMessage(false);

            var extractor = new OverridesExtractor(config);
            var dictionary = extractor.GetOverrides(message);

            Assert.IsTrue(dictionary.ContainsKey(BasicField), string.Format("Validate override for field {0} exists", BasicField));
            Assert.AreEqual(BasicValue, dictionary[BasicField], "Validate the right value was assigned as part of mnemonic resolution");
        }

        [TestMethod]
        public void TestDefaultDateNominalOverride()
        {
            var config = GetConfig();

            config.WorkItemSettings.DateBasedOverrides.Add(new Config.DateBasedFieldOverrides());
            config.WorkItemSettings.DateBasedOverrides[0].FieldName = BasicField;
            config.WorkItemSettings.DateBasedOverrides[0].DefaultValue = BasicValue;
            config.WorkItemSettings.DateBasedOverrides[0].Entries = new List<Config.DateBasedOverrideEntry>
            {
                new Config.DateBasedOverrideEntry
                {
                    StartDate = DateTime.Now + TimeSpan.FromDays(-50),
                    Value = BasicValue
                },
                new Config.DateBasedOverrideEntry
                {
                    StartDate = DateTime.Now + TimeSpan.FromDays(-30),
                    Value = BasicValue2
                },
                new Config.DateBasedOverrideEntry {StartDate = DateTime.Now + TimeSpan.FromDays(10), Value = BasicValue},
                new Config.DateBasedOverrideEntry {StartDate = DateTime.Now + TimeSpan.FromDays(20), Value = BasicValue},
                new Config.DateBasedOverrideEntry {StartDate = DateTime.Now + TimeSpan.FromDays(30), Value = BasicValue},
                new Config.DateBasedOverrideEntry {StartDate = DateTime.Now + TimeSpan.FromDays(40), Value = BasicValue}
            };

            // Should pick this one

            var mailManager = new MailManagerMock();
            var message = mailManager.AddMessage(false);

            var extractor = new OverridesExtractor(config);
            var dictionary = extractor.GetOverrides(message);

            Assert.IsTrue(dictionary.ContainsKey(BasicField), string.Format("Validate override for field {0} exists", BasicField));
            Assert.AreEqual(BasicValue2, dictionary[BasicField], "Validate the right value was assigned as part of mnemonic resolution");
        }

        [TestMethod]
        public void TestDefaultDateNominalAtBeginningOverride()
        {
            var config = GetConfig();

            config.WorkItemSettings.DateBasedOverrides.Add(new Config.DateBasedFieldOverrides());
            config.WorkItemSettings.DateBasedOverrides[0].FieldName = BasicField;
            config.WorkItemSettings.DateBasedOverrides[0].DefaultValue = BasicValue;
            config.WorkItemSettings.DateBasedOverrides[0].Entries = new List<Config.DateBasedOverrideEntry>
            {
                new Config.DateBasedOverrideEntry
                {
                    StartDate = DateTime.Now + TimeSpan.FromDays(-30),
                    Value = BasicValue2
                },
                new Config.DateBasedOverrideEntry {StartDate = DateTime.Now + TimeSpan.FromDays(10), Value = BasicValue},
                new Config.DateBasedOverrideEntry {StartDate = DateTime.Now + TimeSpan.FromDays(20), Value = BasicValue},
                new Config.DateBasedOverrideEntry {StartDate = DateTime.Now + TimeSpan.FromDays(30), Value = BasicValue},
                new Config.DateBasedOverrideEntry {StartDate = DateTime.Now + TimeSpan.FromDays(40), Value = BasicValue}
            };
            // Should pick this one

            var mailManager = new MailManagerMock();
            var message = mailManager.AddMessage(false);

            var extractor = new OverridesExtractor(config);
            var dictionary = extractor.GetOverrides(message);

            Assert.IsTrue(dictionary.ContainsKey(BasicField), string.Format("Validate override for field {0} exists", BasicField));
            Assert.AreEqual(BasicValue2, dictionary[BasicField], "Validate the right value was assigned as part of mnemonic resolution");
        }

        [TestMethod]
        public void TestDefaultDateNominalAtEndOverride()
        {
            var config = GetConfig();

            config.WorkItemSettings.DateBasedOverrides.Add(new Config.DateBasedFieldOverrides());
            config.WorkItemSettings.DateBasedOverrides[0].FieldName = BasicField;
            config.WorkItemSettings.DateBasedOverrides[0].DefaultValue = BasicValue;
            config.WorkItemSettings.DateBasedOverrides[0].Entries = new List<Config.DateBasedOverrideEntry>
            {
                new Config.DateBasedOverrideEntry
                {
                    StartDate = DateTime.Now + TimeSpan.FromDays(-50),
                    Value = BasicValue
                },
                new Config.DateBasedOverrideEntry
                {
                    StartDate = DateTime.Now + TimeSpan.FromDays(-30),
                    Value = BasicValue
                },
                new Config.DateBasedOverrideEntry
                {
                    StartDate = DateTime.Now + TimeSpan.FromDays(-10),
                    Value = BasicValue2
                }
            };

            // Should pick this one

            var mailManager = new MailManagerMock();
            var message = mailManager.AddMessage(false);

            var extractor = new OverridesExtractor(config);
            var dictionary = extractor.GetOverrides(message);

            Assert.IsTrue(dictionary.ContainsKey(BasicField), string.Format("Validate override for field {0} exists", BasicField));
            Assert.AreEqual(BasicValue2, dictionary[BasicField], "Validate the right value was assigned as part of mnemonic resolution");
        }

        [TestMethod]
        public void TestOverloadedMnemonicOverride()
        {
            var config = GetConfig();

            config.WorkItemSettings.Mnemonics.Add(
                new Config.MnemonicDefinition { Mnemonic = BasicMnemonic.ToUpper(), Field = BasicField, Value = BasicValue });

            config.WorkItemSettings.Mnemonics.Add(
                new Config.MnemonicDefinition { Mnemonic = BasicMnemonic.ToUpper(), Field = BasicField2, Value = BasicValue2 });

            var mailManager = new MailManagerMock();
            var message = mailManager.AddMessage(false);
            message.PlainTextBody += string.Format("@@@{0}", BasicMnemonic.ToLower());

            var extractor = new OverridesExtractor(config);
            var dictionary = extractor.GetOverrides(message);

            Assert.IsTrue(dictionary.ContainsKey(BasicField), string.Format("Validate override for field {0} exists", BasicField));
            Assert.IsTrue(dictionary.ContainsKey(BasicField2), string.Format("Validate override for field {0} exists", BasicField2));
            Assert.AreEqual(BasicValue, dictionary[BasicField], "Validate the right value was assigned as part of mnemonic resolution");
            Assert.AreEqual(BasicValue2, dictionary[BasicField2], "Validate the right value was assigned as part of mnemonic resolution");
        }

        [TestMethod]
        public void TestMultiMnemonicSameLine()
        {
            var config = GetConfig();

            config.WorkItemSettings.Mnemonics.Add(
                new Config.MnemonicDefinition { Mnemonic = BasicMnemonic.ToUpper(), Field = BasicField, Value = BasicValue });

            config.WorkItemSettings.Mnemonics.Add(
                new Config.MnemonicDefinition { Mnemonic = BasicMnemonic2.ToUpper(), Field = BasicField2, Value = BasicValue2 });

            var mailManager = new MailManagerMock();
            var message = mailManager.AddMessage(false);
            message.PlainTextBody += string.Format("@@@{0} @@@{1}", BasicMnemonic.ToLower(), BasicMnemonic2.ToLower());

            var extractor = new OverridesExtractor(config);
            var dictionary = extractor.GetOverrides(message);

            Assert.IsTrue(dictionary.ContainsKey(BasicField), string.Format("Validate override for field {0} exists", BasicField));
            Assert.IsTrue(dictionary.ContainsKey(BasicField2), string.Format("Validate override for field {0} exists", BasicField2));
            Assert.AreEqual(BasicValue, dictionary[BasicField], "Validate the right value was assigned as part of mnemonic resolution");
            Assert.AreEqual(BasicValue2, dictionary[BasicField2], "Validate the right value was assigned as part of mnemonic resolution");
        }

        [TestMethod]
        public void TestSimpleExplicitOverride()
        {
            var config = GetConfig();

            var mailManager = new MailManagerMock();
            var message = mailManager.AddMessage(false);
            message.PlainTextBody += string.Format("###{0}:{1}", BasicField, BasicValue);

            var extractor = new OverridesExtractor(config);
            var dictionary = extractor.GetOverrides(message);

            Assert.IsTrue(dictionary.ContainsKey(BasicField), string.Format("Validate override for field {0} exists", BasicField));
            Assert.AreEqual(BasicValue, dictionary[BasicField], "Validate the right value was assigned as part of explicit overrides resolution");
        }

        [TestMethod]
        public void TestSimpleRecipientOverride()
        {
            var config = GetConfig();
            config.WorkItemSettings.RecipientOverrides.Add(
                new Config.RecipientOverrideDefinition {Alias = BasicAlias.ToUpper(), Field = BasicField,Value = BasicValue});

            var mailManager = new MailManagerMock();
            var message = mailManager.AddMessage(false);
            
            // Add the alias that will be resolved
            var newTo = message.ToAddresses.ToList();
            newTo.Add(BasicAlias.ToLower());
            message.ToAddresses = newTo;

            var extractor = new OverridesExtractor(config);
            var dictionary = extractor.GetOverrides(message);

            Assert.IsTrue(dictionary.ContainsKey(BasicField), string.Format("Validate override for field {0} exists", BasicField));
            Assert.AreEqual(BasicValue, dictionary[BasicField], "Validate the right value was assigned as part of recipient overrides resolution");
        }

        [TestMethod]
        public void TestCombinedOverrides()
        {
            const string aliasField = "AliasField";
            const string mnemonicField = "MnemonicField";
            const string explicitField = "ExplicitField";

            const string aliasValue = "AliasValue";
            const string mnemonicValue = "MnemonicValue";
            const string explicitValue = "ExplicitValue";

            var config = GetConfigWithAllOverrides(
                BasicAlias, aliasField, aliasValue,BasicMnemonic, mnemonicField, mnemonicValue);

            var message = GetMessageWithOverrides(BasicAlias, BasicMnemonic, explicitField, explicitValue);

            var extractor = new OverridesExtractor(config);
            var dictionary = extractor.GetOverrides(message);

            Assert.IsTrue(dictionary.ContainsKey(mnemonicField), string.Format("Validate override for field {0} exists (mnemonic)", BasicField));
            Assert.IsTrue(dictionary.ContainsKey(explicitField), string.Format("Validate override for field {0} exists (explicit)", BasicField));
            Assert.IsTrue(dictionary.ContainsKey(aliasField), string.Format("Validate override for field {0} exists (recipient)", BasicField));
            Assert.AreEqual(mnemonicValue, dictionary[mnemonicField], "Validate the right value was assigned as part of mnemonic overrides resolution");
            Assert.AreEqual(explicitValue, dictionary[explicitField], "Validate the right value was assigned as part of explicit overrides resolution");
            Assert.AreEqual(aliasValue, dictionary[aliasField], "Validate the right value was assigned as part of recipient overrides resolution");
        }

        [TestMethod]
        public void TestOverridesPrecedenceExplicitTrumps()
        {
            const string aliasValue = "AliasValue";
            const string mnemonicValue = "MnemonicValue";
            const string explicitValue = "ExplicitValue";

            var config = GetConfigWithAllOverrides(
                BasicAlias, BasicField, aliasValue, BasicMnemonic, BasicField, mnemonicValue);

            var message = GetMessageWithOverrides(BasicAlias, BasicMnemonic, BasicField, explicitValue);

            var extractor = new OverridesExtractor(config);
            var dictionary = extractor.GetOverrides(message);

            Assert.IsTrue(dictionary.ContainsKey(BasicField), string.Format("Validate override for field {0} exists (mnemonic)", BasicField));
            Assert.AreEqual(explicitValue, dictionary[BasicField], "Validate the right value was assigned as part of explicit overrides resolution");
        }

        [TestMethod]
        public void TestOverridesPrecedenceMnemonicTrumps()
        {
            const string aliasValue = "AliasValue";
            const string mnemonicValue = "MnemonicValue";

            var config = GetConfigWithAllOverrides(
                BasicAlias, BasicField, aliasValue, BasicMnemonic, BasicField, mnemonicValue);

            var message = GetMessageWithOverrides(BasicAlias, BasicMnemonic, null, null);

            var extractor = new OverridesExtractor(config);
            var dictionary = extractor.GetOverrides(message);

            Assert.IsTrue(dictionary.ContainsKey(BasicField), string.Format("Validate override for field {0} exists (mnemonic)", BasicField));
            Assert.AreEqual(mnemonicValue, dictionary[BasicField], "Validate the right value was assigned as part of explicit overrides resolution");
        }

        private static IncomingEmailMessageMock GetMessageWithOverrides(string alias, string mnemonic, string explicitField, string explicitValue)
        {
            var mailManager = new MailManagerMock();
            var message = mailManager.AddMessage(false);

            if (!string.IsNullOrEmpty(mnemonic))
            {
                message.PlainTextBody += string.Format("@@@{0}\n", mnemonic);
            }

            if (!string.IsNullOrEmpty(explicitField))
            {
                message.PlainTextBody += string.Format("###{0}:{1}\n", explicitField, explicitValue);
            }

            if (!string.IsNullOrEmpty(alias))
            {
                var newTo = message.ToAddresses.ToList();
                newTo.Add(BasicAlias);
                message.ToAddresses = newTo;
            }

            return message;
        }

        private static Config.InstanceConfig GetConfigWithAllOverrides(
            string alias, string aliasField, string aliasValue,
            string mnemonic, string mnemonicField, string mnemonicValue)
        {
            var config = GetConfig();

            config.WorkItemSettings.Mnemonics.Add(
                new Config.MnemonicDefinition { Mnemonic = mnemonic, Field = mnemonicField, Value = mnemonicValue });
            config.WorkItemSettings.RecipientOverrides.Add(
                new Config.RecipientOverrideDefinition { Alias = alias, Field = aliasField, Value = aliasValue });

            return config;
        }

        private static Config.InstanceConfig GetConfig()
        {
            var config = new Config.InstanceConfig
            {
                WorkItemSettings = new Config.WorkItemSettings
                {
                    Mnemonics = new List<Config.MnemonicDefinition>(),
                    RecipientOverrides = new List<Config.RecipientOverrideDefinition>(),
                    DateBasedOverrides = new List<Config.DateBasedFieldOverrides>()
                },
                EmailSettings = new Config.EmailSettings
                {
                    ExplicitOverridesRegex = @"###\s*(?<fieldName>[^:]*):\s*(?<value>.*)"
                }
               
            };

            return config;
        }

        private const string BasicMnemonic = "ABCDMnemonic";
        private const string BasicMnemonic2 = "1234Mnemonic";
        private const string BasicAlias = "ABCDAlias";
        private const string BasicField = "ABCD Field";
        private const string BasicValue = "ABCD Value";
        private const string BasicField2 = "123";
        private const string BasicValue2 = "567";
    }
}
