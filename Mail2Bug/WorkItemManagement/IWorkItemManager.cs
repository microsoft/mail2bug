using System;
using System.Collections.Generic;
using Mail2Bug.Email;
using Mail2Bug.MessageProcessingStrategies;

namespace Mail2Bug.WorkItemManagement
{
    public interface IWorkItemManager
    {
        void AttachFiles(int workItemId, IEnumerable<string> fileList);

        void AttachAndInlineFiles(int workItemId, IEnumerable<Tuple<string, IIncomingEmailAttachment>> fileList);

        SortedList<string, int> WorkItemsCache { get; }

        void CacheWorkItem(int workItemId);

        /// <param name="values">Field Values</param>
        /// <returns>Bug ID</returns>
        int CreateWorkItem(Dictionary<string, string> values);

        /// <param name="workItemId">The ID of the bug to modify </param>
        /// <param name="comment">Comment to add to description</param>
        /// <param name="values">List of fields to change</param>
        void ModifyWorkItem(int workItemId, string comment, Dictionary<string, string> values);

        INameResolver GetNameResolver();

        /// <summary>
        /// Get fields for the work item with the given ID.
        /// </summary>
        /// <param name="workItemId">the work item ID</param>
        /// <returns>work item corresponding to the ID</returns>
        IWorkItemFields GetWorkItemFields(int workItemId);
    }
}
