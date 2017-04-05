using System.Collections.Generic;
using System.Text;
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
                var value = specialReplacements.ContainsKey(fieldNameUpper)
                    ? specialReplacements[fieldNameUpper]
                    : workItemFields.GetFieldValue(placeholder.Field);
                bodyBuilder.Replace(placeholder.Text, value ?? placeholder.DefaultValue);
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
                ["MAIL2BUGALIAS"] = config.EmailSettings.Recipients?.Replace(';', '/'),

                // TFS specific fields.
                ["TFSWORKITEMTEMPLATE"] = config.TfsServerConfig.WorkItemTemplate,
                ["TFSCOLLECTIONURI"] = config.TfsServerConfig.CollectionUri,
                ["TFSPROJECT"] = config.TfsServerConfig.Project,

                // Special work item fields.
                ["BUGID"] = workItemFields.ID,  // For backward compat
                ["_ID"] = workItemFields.ID,
                ["_TITLE"] = workItemFields.Title,
                ["_OWNER"] = workItemFields.Owner,
                ["_STATE"] = workItemFields.State
            };
        }
    }
}
