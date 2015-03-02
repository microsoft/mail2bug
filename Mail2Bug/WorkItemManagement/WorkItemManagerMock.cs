using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using log4net;
using Mail2Bug.MessageProcessingStrategies;

namespace Mail2Bug.WorkItemManagement
{
    public class WorkItemManagerMock :IWorkItemManager, IDisposable
    {
        private readonly string _keyField;

        static WorkItemManagerMock()
        {
            XmlWriterSettings = new XmlWriterSettings
                {
                    NewLineChars = "\r\n",
                    Encoding = Encoding.UTF8,
                    Indent = true,
                    IndentChars = "\t",
                    CheckCharacters = false
                };
        }

        public WorkItemManagerMock(string keyField, INameResolver resolver = null)
        {
            _keyField = keyField;
            _resolver = resolver ?? new NameResolverMock();

            WorkItemsCache = new SortedList<string, int>();
        }

        public void AttachFiles(int workItemId, List<string> fileList)
        {
            foreach (var filename in fileList.Where(filename => !File.Exists(filename)))
            {
                Logger.ErrorFormat("Couldn't find attachment file {0}", filename);
            }

            if (ThrowOnAttachFiles != null)
            {
                throw ThrowOnAttachFiles;
            }

            if (!Attachments.ContainsKey(workItemId))
            {
                Attachments[workItemId] = new List<string>();
            }

            Attachments[workItemId].AddRange(fileList);
        }

        public void CacheWorkItem(int workItemId)
        {
            if (ThrowOnCacheWorkItem != null) throw ThrowOnCacheWorkItem;

            var workItem = Bugs[workItemId];
            if (!workItem.ContainsKey(_keyField))
            {
                workItem[_keyField] = workItemId.ToString(CultureInfo.InvariantCulture);
            }
            
            var conversationId = workItem[_keyField];
            WorkItemsCache[conversationId] = workItemId;
        }

        public int CreateWorkItem(Dictionary<string, string> values)
        {
            if (ThrowOnCreateBug != null) throw ThrowOnCreateBug;

            Logger.InfoFormat("Creating bug:");
            
            // Generate a random ID
            int id;
            do
            {
                id = _rand.Next(1, int.MaxValue);
            } while (Bugs.ContainsKey(id));

            Bugs[id] = new Dictionary<string, string>(values);

            CacheWorkItem(id);
            
            return id;
        }

        private void SerializeBugs()
        {
            try
            {
                Logger.DebugFormat("Serializing bugs to file {0}", Path.GetFullPath(SerializationFilename));

                using (var fs = new FileStream(SerializationFilename, FileMode.Create, FileAccess.Write))
                {
                    using (var writer = XmlWriter.Create(fs,XmlWriterSettings))
                    {
                        writer.WriteStartElement("Bugs");

                        foreach (var bug in Bugs)
                        {
                            Logger.DebugFormat("Processing bug {0} with {1} values", bug.Key, bug.Value.Count);
                            writer.WriteStartElement("WorkItem");
                            writer.WriteAttributeString("ID", bug.Key.ToString(CultureInfo.InvariantCulture));

                            foreach (var val in bug.Value)
                            {
                                Logger.DebugFormat("Bug {0}: Writing value {1},{2}", bug.Key, val.Key, val.Value);
                                writer.WriteElementString(GetValidXmlElementName(val.Key), val.Value);
                            }

                            Logger.DebugFormat("Finished processing bug {0}", bug.Key);
                            writer.WriteEndElement();
                        }

                        Logger.DebugFormat("Finished processing all bugs");
                        writer.WriteEndElement();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
        }

        private static string GetValidXmlElementName(string val)
        {
            var validName = Regex.Replace(val, @"[\s()]", "_");
            Logger.DebugFormat("{0} --> {1}", val, validName);
            return validName;
        }

        public void ModifyWorkItem(int workItemId, string comment, Dictionary<string, string> values)
        {
            if (ThrowOnModifyBug != null) throw ThrowOnModifyBug;

            if (!Bugs.ContainsKey(workItemId))
            {
                Logger.WarnFormat("Trying to modify non-existing bug {0}. Initializing with no field values", workItemId);
                Bugs[workItemId] = new Dictionary<string, string>();
            }

            var bugEntry = Bugs[workItemId];
            foreach (var key in values.Keys)
            {
                bugEntry[key] = values[key];
            }

            if (!bugEntry.ContainsKey(HistoryField))
            {
                bugEntry[HistoryField] = "";
            }

            bugEntry[HistoryField] += comment;
        }

        public INameResolver GetNameResolver()
        {
            if (ThrowOnGetNameResolver != null) throw ThrowOnGetNameResolver;
            return _resolver;

        }

        public void Dispose()
        {
            SerializeBugs();
        }

        public SortedList<string, int> WorkItemsCache { get; set; }

        public Dictionary<int, Dictionary<string, string>> Bugs = new Dictionary<int, Dictionary<string, string>>(); 
        public Dictionary<int, List<string>> Attachments = new Dictionary<int, List<string>>();

        public Exception ThrowOnAttachFiles { get; set; }
        public Exception ThrowOnCacheBugs { get; set; }
        public Exception ThrowOnCacheWorkItem { get; set; }
        public Exception ThrowOnCreateBug { get; set; }
        public Exception ThrowOnModifyBug { get; set; }
        public Exception ThrowOnGetNameResolver { get; set; }

        readonly Random _rand = new Random();
        private readonly INameResolver _resolver;
        private static readonly string SerializationFilename = Path.Combine(Directory.GetCurrentDirectory(),"SerializedBugs.xml");
        public const string HistoryField = "History";

        private static readonly XmlWriterSettings XmlWriterSettings;

        private static readonly ILog Logger = LogManager.GetLogger(typeof(WorkItemManagerMock));
    }
}
