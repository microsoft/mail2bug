using System;
using System.Xml;
using Microsoft.TeamFoundation.Framework.Common;

namespace Mail2Bug.WorkItemManagement
{
    public class TFSQueryParser
    {
        private const string QueryElementTag = "Wiql";

        public static string ParseQueryFile(string rawQueryFileContents)
        {
            //if (String.IsNullOrEmpty(filename))
            //{
            //    throw new ConfigFileException("No cache query file specified");
            //}

            // Parse the WIQ file and return the actual query text
            var doc = new XmlDocument();
            doc.LoadXml(rawQueryFileContents);
            var queryTextElement = doc.GetElementsByTagName(QueryElementTag);

            if (queryTextElement == null || queryTextElement.Count == 0)
            {
                throw new ConfigFileException("Bad query file format - no 'Wiql' element");
            }

            return queryTextElement[0].InnerText;            
        }
    }
}
