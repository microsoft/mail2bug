using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using log4net;
using Mail2Bug.Email;

namespace Mail2Bug.MessageProcessingStrategies
{
    /// <summary>
    /// This class is responsible for processing the message and extracting the list of all the fields that should be overridden
    /// and the values with which they should be overridden.
    /// These values can then be applied to the work item by the message processing strategy.
    /// </summary>
    public class OverridesExtractor
    {
        private readonly Config.InstanceConfig _config;
        private Dictionary<string, DateBasedValueResolver> _dateBasedResolvers;

        public OverridesExtractor(Config.InstanceConfig config)
        {
            _config = config;

            InitDateBasedResolvers();
        }

        private void InitDateBasedResolvers()
        {
            _dateBasedResolvers = new Dictionary<string, DateBasedValueResolver>();

            if (_config.WorkItemSettings.DateBasedOverrides == null)
            {
                return;
            }

            foreach (var overrides in _config.WorkItemSettings.DateBasedOverrides)
            {
                var entries = new SortedList<DateTime, string>();
                overrides.Entries.ForEach(x => entries.Add(x.StartDate, x.Value));

                _dateBasedResolvers.Add(overrides.FieldName, new DateBasedValueResolver(overrides.DefaultValue, entries));
            }
        }

        public Dictionary<string, string> GetOverrides(IIncomingEmailMessage message)
        {
            var result = new Dictionary<string, string>();
            AddToDictionary(GetRecipientOverrides(message), result);
            AddToDictionary(GetDatebasedOverrides(), result);
            AddToDictionary(GetMnemonicOverrides(GetMessageFullText(message)), result);
            AddToDictionary(GetExplicitOverrides(GetMessageFullText(message)), result);

            return result;
        }

        public Dictionary<string, string> GetOverrides(string text)
        {
            var result = new Dictionary<string, string>();
            AddToDictionary(GetMnemonicOverrides(text), result);
            AddToDictionary(GetExplicitOverrides(text), result);

            return result;
        }

        private IEnumerable<KeyValuePair<string, string>> GetDatebasedOverrides()
        {
            var overrides = new List<KeyValuePair<string, string>>();
            foreach (var resolver in _dateBasedResolvers)
            {
                overrides.Add(new KeyValuePair<string, string>(resolver.Key, resolver.Value.Resolve(DateTime.Now)));
            }

            return overrides;
        }

        private IEnumerable<KeyValuePair<string, string>> GetRecipientOverrides(IIncomingEmailMessage message)
        {
            var overrides = new List<KeyValuePair<string, string>>();
            overrides.AddRange(message.CcAddresses.SelectMany(ExtractRecipientOverrides));
            overrides.AddRange(message.ToAddresses.SelectMany(ExtractRecipientOverrides));

            return overrides;
        }

        private IEnumerable<KeyValuePair<string,string>> ExtractRecipientOverrides(string recipientAlias)
        {
            if (_config.WorkItemSettings.RecipientOverrides == null)
            {
                return new Dictionary<string, string>();
            }

            var overrides = 
                from def in _config.WorkItemSettings.RecipientOverrides
                where def.Alias.Equals(recipientAlias, StringComparison.InvariantCultureIgnoreCase)
                select new KeyValuePair<string, string>(def.Field, def.Value);

            var overridesList = overrides as List<KeyValuePair<string, string>> ?? overrides.ToList();
            foreach (var entry in overridesList)
            {
                Logger.InfoFormat("Found override {0}={1} for alias {2}", entry.Key, entry.Value, recipientAlias);
            }

            return overridesList;
        }

        /// <summary>
        /// Adds the elements from 'elementsToAdd' to the dictionary.
        /// If an element already exists in the dictionary, its value is overwritten by the new value.
        /// </summary>
        private static void AddToDictionary(IEnumerable<KeyValuePair<string, string>> elementsToAdd, Dictionary<string, string> dictionary)
        {
            elementsToAdd.ToList().ForEach(x => dictionary[x.Key] = x.Value);
        }

        /// <summary>
        /// Gets the list of key-value pairs for fields overridden by mnemonics
        /// </summary>
        /// <returns></returns>
        private IEnumerable<KeyValuePair<string, string>> GetMnemonicOverrides(string text)
        {
            return GetMnemonics(text).SelectMany(ResolveMnemonic);
        }

        private string GetMessageFullText(IIncomingEmailMessage message)
        {
            var sb = new StringBuilder();
            sb.AppendLine(message.Subject).AppendLine(message.PlainTextBody);
            return sb.ToString();
        }

        /// <summary>
        /// Gets a list of all the mnemonics that appear in the text
        /// </summary>
        private static IEnumerable<string> GetMnemonics(string text)
        {
            var mnemonicsRegex = new Regex(@"@@@\s*(?<mnemonic>\w+)");
            var mnemonics = from Match match in mnemonicsRegex.Matches(text) select match.Groups["mnemonic"].Value;
            return mnemonics;
        }

        /// <summary>
        /// Resolves a mnemonic to the list of fields it should set and their appropriate values
        /// </summary>
        private IEnumerable<KeyValuePair<string, string>> ResolveMnemonic(string mnemonic)
        {
            var mnemonicEntries = _config.WorkItemSettings.Mnemonics.FindAll(x => x.Mnemonic.Equals(mnemonic, StringComparison.InvariantCultureIgnoreCase));

            if (!mnemonicEntries.Any())
            {
                Logger.WarnFormat("Unknown mnemonic used - '{0}'", mnemonic);
                return new List<KeyValuePair<string, string>>();
            }

            mnemonicEntries.ForEach(entry => Logger.InfoFormat("Mnemonic {0} resolved to {1}={2}", mnemonic, entry.Field, entry.Value));
            return mnemonicEntries.Select(x => new KeyValuePair<string,string>(x.Field, x.Value));
        }

        /// <summary>
        /// Gets a list of all the explicit overrides in the message
        /// </summary>
        private IEnumerable<KeyValuePair<string, string>> GetExplicitOverrides(string text)
        {
            var overridesRegex = new Regex(_config.EmailSettings.ExplicitOverridesRegex);

            var overrides =
                from Match match in overridesRegex.Matches(text)
                select ExtractOverride(match);

            return overrides;
        }

        /// <summary>
        /// Helper function to transform regex match to a friendly form for handling overrides
        /// </summary>
        private static KeyValuePair<string, string> ExtractOverride(Match match)
        {
            var key = match.Groups["fieldName"].Value.Trim();
            var value = match.Groups["value"].Value.Trim();

            Logger.InfoFormat("Found explicit override {0}={1}", key, value);
            return new KeyValuePair<string, string>(key, value);
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(OverridesExtractor));
    }
}
