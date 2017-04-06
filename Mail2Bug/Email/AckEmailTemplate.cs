using System.Collections.Generic;
using System.Text;
using log4net;
using Mail2Bug.WorkItemManagement;

namespace Mail2Bug.Email
{
    /// <summary>
    /// Reply email template.
    /// </summary>
    public class AckEmailTemplate
    {
        private string _text;

        public AckEmailTemplate(string templateText)
        {
            _text = templateText;
        }

        /// <summary>
        /// Produce text content by applying replacements on the template.
        /// </summary>
        /// <param name="workItemFields">work item for which the content is generated</param>
        /// <param name="config">the instance config</param>
        /// <returns>text content produced by applying replacements for placeholders</returns>
        public string Apply(IWorkItemFields workItemFields, Config.InstanceConfig config)
        {
            var bodyBuilder = new StringBuilder(_text);
            var placeholders = AckEmailPlaceholder.GetPlaceholders(_text);
            var specialReplacements = GetSpecialReplacements(workItemFields, config);

            foreach (var placeholder in placeholders)
            {
                var fieldNameUpper = placeholder.Field.ToUpper();
                var fieldValue = specialReplacements.ContainsKey(fieldNameUpper)
                    ? specialReplacements[fieldNameUpper]
                    : workItemFields.GetFieldValue(placeholder.Field);
                var value = string.IsNullOrWhiteSpace(fieldValue) ? placeholder.DefaultValue : fieldValue;
                Logger.DebugFormat("Replacing placeholder {0} with value '{1}'", placeholder.Text, value);
                bodyBuilder.Replace(placeholder.Text, value);
            }
            
            return bodyBuilder.ToString();
        }

        /// <summary>
        /// Get replacements to use for special placeholders. This includes provider independent placeholders for standard 
        /// work item fields as well as global placeholders that are not work item specific.
        /// </summary>
        /// <param name="workItemFields">work item for which template is to be generated</param>
        /// <param name="config">the instance config</param>
        /// <returns></returns>
        private IDictionary<string, string> GetSpecialReplacements(IWorkItemFields workItemFields, Config.InstanceConfig config)
        {
            return new Dictionary<string, string>
            {
                // Non-work item fields.
                ["MAIL2BUGALIAS"] = EncodeHtml(config.EmailSettings.Recipients?.Replace(';', '/')),

                // TFS specific fields.
                ["TFSWORKITEMTEMPLATE"] = EncodeHtml(config.TfsServerConfig.WorkItemTemplate),
                ["TFSCOLLECTIONURI"] = EncodeHtml(config.TfsServerConfig.CollectionUri),
                ["TFSPROJECT"] = EncodeHtml(config.TfsServerConfig.Project),

                // Special work item fields.
                ["BUGID"] = EncodeHtml(workItemFields.ID),  // for backward compat
                [AckEmailPlaceholder.ID] = EncodeHtml(workItemFields.ID),
                [AckEmailPlaceholder.Title] = EncodeHtml(workItemFields.Title),
                [AckEmailPlaceholder.Owner] = EncodeHtml(workItemFields.Owner),
                [AckEmailPlaceholder.State] = EncodeHtml(workItemFields.State)
            };
        }

        /// <summary>
        /// Encode field values for use in HTML.
        /// </summary>
        /// <param name="value">the string to encode</param>
        /// <returns>encoded string after handling HTML entities</returns>
        private string EncodeHtml(string value)
        {
            return new StringBuilder(value)
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .ToString();
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(AckEmailTemplate));
    }
}
