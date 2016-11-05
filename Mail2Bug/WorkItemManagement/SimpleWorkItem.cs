using System.Collections.Generic;

namespace Mail2Bug.WorkItemManagement
{
    public class SimpleWorkItem
    {
        /// <summary>
        /// The work item ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Work item title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Current owner of the work item.
        /// </summary>
        public string AssignedTo { get; set; }

        /// <summary>
        /// State of the work item.
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// All the work item fields, as a dictionary.
        /// </summary>
        public Dictionary<string, string> Fields { get; set; }
    }
}
