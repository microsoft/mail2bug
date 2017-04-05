﻿using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Mail2Bug.WorkItemManagement
{
    /// <summary>
    /// IWorkItemFields implementation for TFS/VSO. 
    /// Provides access to fields of a TFS work item.
    /// </summary>
    public class TFSWorkItemFields : IWorkItemFields
    {
        private WorkItem _workItem;
        
        public string ID => GetFieldValue("ID");

        public string Title => GetFieldValue("Title");

        public string State => GetFieldValue("State");

        public string Owner => GetFieldValue("Assigned To");

        public TFSWorkItemFields(WorkItem workItem)
        {
            _workItem = workItem;
        }

        public string GetFieldValue(string fieldName)
        {
            return _workItem.Fields[fieldName]?.ToString();
        }
    }
}
