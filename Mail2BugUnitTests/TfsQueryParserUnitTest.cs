using System;
using System.Collections.Generic;
using Mail2Bug.WorkItemManagement;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mail2BugUnitTests
{
    [TestClass]
    public class TfsQueryParserUnitTest
    {
        [TestMethod]
        public void TestBasicQueries()
        {
            var testCases = new List<Tuple<string, string>>
            {
                new Tuple<string, string>(
                    @"<?xml version=""1.0"" encoding=""utf-8""?><WorkItemQuery Version=""1""><TeamFoundationServer>http://myserver:8080/tfs/abcd</TeamFoundationServer>" +
                    @"<TeamProject>Project</TeamProject><Wiql>SELECT [System.Id], [System.Title] FROM WorkItems WHERE [System.WorkItemType] &lt;&gt; ''  AND ([System.CreatedBy] =" +
                    @" 'mail2bug' OR [System.CreatedBy] = 'mail2buguser') AND  [System.ChangedDate] > @today - 60 ORDER BY [System.Id]</Wiql></WorkItemQuery>",

                    "SELECT [System.Id], [System.Title] FROM WorkItems WHERE [System.WorkItemType] <> ''  AND ([System.CreatedBy] = 'mail2bug' OR [System.CreatedBy] " +
                    "= 'mail2buguser') AND  [System.ChangedDate] > @today - 60 ORDER BY [System.Id]"),
                new Tuple<string, string>(
                    @"<?xml version=""1.0"" encoding=""utf-8""?><WorkItemQuery Version=""1""><TeamFoundationServer>http://myserver:8080/tfs/abcd</TeamFoundationServer>" +
                    @"<TeamProject>Project</TeamProject><Wiql>SELECT [System.Id], [System.Title] FROM WorkItems WHERE ([System.WorkItemType] = 'Bug' OR " +
                    @"[System.WorkItemType] = 'Task')  AND ([System.CreatedBy] = 'mail2bug' OR [System.CreatedBy] = 'mail2buguser') AND  [System.ChangedDate] &gt; " +
                    @"@today - 60 ORDER BY [System.Id]</Wiql></WorkItemQuery>",

                    "SELECT [System.Id], [System.Title] FROM WorkItems WHERE ([System.WorkItemType] = 'Bug' OR [System.WorkItemType] = 'Task')  AND ([System.CreatedBy] = " +
                    "'mail2bug' OR [System.CreatedBy] = 'mail2buguser') AND  [System.ChangedDate] > @today - 60 ORDER BY [System.Id]")
            };

            foreach (var testCase in testCases)
            {
                Assert.AreEqual(testCase.Item2, TFSQueryParser.ParseQueryFile(testCase.Item1));
            }
        }
    }
}
