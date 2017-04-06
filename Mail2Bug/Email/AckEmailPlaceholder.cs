using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Mail2Bug.Email
{
    /// <summary>
    /// Placeholders in the email template; of format [Field] or [Field:Default Value].
    /// These will be replaced by the corrpesonding fields from the work item.
    /// </summary>
    public class AckEmailPlaceholder
    {
        /// <summary>
        /// Regex pattern for placeholders.
        /// </summary>
        private const string _pattern = @"\[([^:\]]+)(:([^\]]*))?\]";

        /// <summary>
        /// Provider independent placeholder for work item ID.
        /// </summary>
        public const string ID = "_ID";

        /// <summary>
        /// Provider independent placeholder for work item title.
        /// </summary>
        public const string Title = "_TITLE";

        /// <summary>
        /// Provider independent placeholder for work item owner.
        /// </summary>
        public const string Owner = "_OWNER";

        /// <summary>
        /// Provider independent placeholder for work item state.
        /// </summary>
        public const string State = "_STATE";

        /// <summary>
        /// Verbatim text for the placeholder, as used in the template.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Name of the field being referenced.
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// Default value provided for the field reference.
        /// </summary>
        public string DefaultValue { get; set; }

        /// <summary>
        /// Extract placeholders from the given template.
        /// </summary>
        /// <param name="template">the template to parse.</param>
        /// <returns>placeholders extracted from the template.</returns>
        public static IList<AckEmailPlaceholder> GetPlaceholders(string template)
        {
            var regex = new Regex(_pattern);
            var matches = regex.Matches(template);
            var placeholders = new List<AckEmailPlaceholder>(matches.Count);

            foreach (Match match in matches)
            {
                var text = match.Groups[0].Value;
                var field = match.Groups[1].Value;
                var defaultValue = match.Groups[3].Value;
                var placeholder = new AckEmailPlaceholder(text, field, defaultValue);
                placeholders.Add(placeholder);
            }

            return placeholders;
        }

        public AckEmailPlaceholder(string text, string variable, string defaultValue)
        {
            Text = text;
            Field = variable;
            DefaultValue = defaultValue;
        }
        
        public override string ToString()
        {
            return Text;
        }
    }
}
