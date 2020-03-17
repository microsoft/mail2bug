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
        public static string GetLastMessageText(IIncomingEmailMessage message, bool enableExperimentalHtmlFeatures)
        {
            return enableExperimentalHtmlFeatures && message.IsHtmlBody ? GetLastMessageText_Html(message.RawBody) : GetLastMessageText_PlainText(message);
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

            const string outlookDesktopSeparatorStyle = "border:none;border-top:solid #E1E1E1 1.0pt;padding:3.0pt 0in 0in 0in";
            const string outlookMobileSeparatorStyle = "display:inline-block;width:98%";

            // There's no well-defined way to parse the latest email from a thread
            // We have to use heuristics to cover different email clients
            foreach (IDomObject element in dom["*"])
            {
                // Lots of email clients insert html elements as message delimiters which have styling but no inner text
                // This block checks for some of these patterns
                if (string.Equals(element.NodeName, "div", StringComparison.OrdinalIgnoreCase) &&
                   (element.Id == "divRplyFwdMsg" || element.Id == "x_divRplyFwdMsg" || outlookDesktopSeparatorStyle.Equals(element.GetAttribute("style"))))
                {
                    IDomContainer parent = element.ParentNode;
                    RemoveSubsequent(parent);
                    parent.Remove();
                    break;
                }

                if (string.Equals(element.NodeName, "hr", StringComparison.OrdinalIgnoreCase) &&
                    outlookMobileSeparatorStyle.Equals(element.GetAttribute("style")))
                {
                    RemoveSubsequent(element);
                    element.Remove();
                    break;
                }

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

        /// <summary>
        /// If an email embeds an email inline in its html, that embedded image won't display correctly unless we modify the html.
        /// This method does that, given information about email's known attachments
        /// </summary>
        public static string UpdateEmbeddedImageLinks(string originalHtml, System.Collections.Generic.IReadOnlyCollection<MessageAttachmentInfo> attachments)
        {
            if (attachments == null || attachments.Count == 0)
            {
                return originalHtml;
            }

            CQ dom = originalHtml;
            foreach (var attachment in attachments)
            {
                string originalImgSrc = $"cid:{attachment.ContentId}";
                var matchingImgLinks = dom[$"img[src$='{originalImgSrc}']"];

                // This may point to the file on the local file-system if we haven't yet uploaded the attachment
                // However, the work item APIs seem to magically 'just work' with this and update the URI to point to the uploaded location
                // If for some reason that stops working, we'd need to either infer the uploaded URI or upload first and mutate the html afterward
                var newSrc = new Uri(attachment.FilePath);
                foreach (IDomObject img in matchingImgLinks)
                {
                    img.SetAttribute("src", newSrc.AbsoluteUri);
                }
            }

            return dom.Render();
        }
    }
}
