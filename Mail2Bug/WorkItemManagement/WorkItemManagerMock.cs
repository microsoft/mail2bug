using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using log4net;
using Mail2Bug.MessageProcessingStrategies;

namespace Mail2Bug.WorkItemManagement
{
    public class WorkItemManagerMock :IWorkItemManager
    {
        private readonly string _keyField;

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
        public const string HistoryField = "History";

        private static readonly ILog Logger = LogManager.GetLogger(typeof(WorkItemManagerMock));
    }
}
