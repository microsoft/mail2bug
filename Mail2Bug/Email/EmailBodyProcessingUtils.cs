using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CsQuery;

namespace Mail2Bug.Email
{
    public class EmailBodyProcessingUtils
    {
        public static string GetLastMessageText(IIncomingEmailMessage message)
        {
            return message.IsHtmlBody ? GetLastMessageText_Html(message.RawBody) : GetLastMessageText_PlainText(message);
        }

        private static string GetLastMessageText_PlainText(IIncomingEmailMessage message)
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

        public static string GetLastMessageText_Html(string rawBody)
        {
            CQ dom = rawBody;

            foreach (IDomObject element in dom["*"])
            {
                if (!element.ChildElements.Any() && !string.IsNullOrWhiteSpace(element.InnerText))
                {
                    var separatorIndex = IndexOfAny(element.InnerText, MessageBorderMarkers);
                    if (separatorIndex.HasValue)
                    {
                        element.InnerText = element.InnerText.Substring(0, separatorIndex.Value);
                        RemoveSubsequent(element);
                        break;
                    }
                }
            }

            return dom.Render();
        }

        private static void RemoveSubsequent(IDomObject element)
        {
            IDomObject nextNode;
            while ((nextNode = element.NextSibling) != null)
            {
                nextNode.Remove();
            }

            if (element.ParentNode is IDomObject parentElement)
            {
                RemoveSubsequent(parentElement);
            }
        }

        private static readonly string[] MessageBorderMarkers =
        {
            "_____",
            "-----Original Message",
            "From:",
        };

        /// <summary>
        /// Get the seperate denotation between reply content and original content.
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        private static int GetReplySeperatorIndex(string description)
        {
            return IndexOfAny(description, MessageBorderMarkers) ?? description.Length;
        }

        private static int? IndexOfAny(string text, IEnumerable<string> searchTerms)
        {
            return searchTerms
                .Select(search => text.IndexOf(search, StringComparison.Ordinal))
                .Where(index => index >= 0)
                .OrderBy(index => index)
                .Cast<int?>()
                .FirstOrDefault();
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

        public static string FixUpImgLinks(string description, IDictionary<string, string> messageContentIdToTfsGuid)
        {
            CQ dom = description;
            foreach (var pair in messageContentIdToTfsGuid)
            {
                string contentId = pair.Key;
                string guid = pair.Value;

                string originalImgSrc = $"cid:{contentId}";
                var matchingImgLinks = dom[$"img[src$='{originalImgSrc}']"];
                foreach (IDomObject img in matchingImgLinks)
                {
                    img.SetAttribute("src", guid);
                }
            }

            return dom.Render();
        }
    }
}
