using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Mail2Bug.Email
{
    public class EmailBodyProcessingUtils
    {
        public static string GetLastMessageText(IIncomingEmailMessage message)
        {
            var lastMessage = new StringBuilder();
            lastMessage.Append(message.PlainTextBody);

            var next = GetReplySeperatorIndex(lastMessage.ToString());

            if (next > 0)
            {
                lastMessage.Remove(next, lastMessage.Length - next);
            }

            return lastMessage.ToString();
        }

        /// <summary>
        /// Get the seperate denotation between reply content and original content.
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        private static int GetReplySeperatorIndex(string description)
        {
            var indices = new List<int> 
            {
                description.IndexOf("_____", StringComparison.Ordinal),
                description.IndexOf("-----Original Message", StringComparison.Ordinal),
                description.IndexOf("From:", StringComparison.Ordinal)
            };

            indices.RemoveAll(x => x < 0);
            if (indices.Count == 0)
            {
                return description.Length;
            }

            return indices.Min();
        }

        public static string ConvertHtmlMessageToPlainText(string text)
        {
            // First remove comments, since they may have nested tags in them, which throw off the tag stripper regex
            text = Regex.Replace(text, "<!--.*?-->", string.Empty, RegexOptions.Singleline);

            // Remove invisible html elements
            text = Regex.Replace(text, "<head>.*</head>", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, "<title>.*</title>", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, "<style>.*</style>", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, "<script>.*</script>", string.Empty, RegexOptions.Singleline);

            // Remove tags and translate unicode entities
            text = Regex.Replace(text, "<.*?>", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"&#(?<ordinal>\d+);", ResolveOridnalToChar); 

            var sb = new StringBuilder(text.Trim());

            // Now unescape chars
            sb.Replace("&quot;", "\"");
            sb.Replace("&gt;", ">");
            sb.Replace("&lt;", "<");
            sb.Replace("&nbsp;", " ");
            sb.Replace("&amp;", "&");

            return Regex.Replace(sb.ToString(), "[\r\n]{2,}", "\n");
        }

        private static string ResolveOridnalToChar(Match ordinalMatch)
        {
            var unicodeChar = (char)int.Parse(ordinalMatch.Groups["ordinal"].Value);
            return new string(new[] {unicodeChar});
        }
    }
}
