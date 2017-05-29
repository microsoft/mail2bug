using System.Collections.Generic;

namespace Mail2Bug.WorkItemManagement
{
    public class WorkItemFieldsMock : IWorkItemFields
    {
        private IDictionary<string, string> fields;

        public WorkItemFieldsMock(IDictionary<string, string> fields)
        {
            this.fields = fields;
        }

        public string ID => GetFieldValue("ID");

        public string Title => GetFieldValue("Title");

        public string State => GetFieldValue("State");

        public string Owner => GetFieldValue("Assigned To");

        public string GetFieldValue(string fieldName)
        {
            return fields.ContainsKey(fieldName) ? fields[fieldName] : null;
        }
    }
}
