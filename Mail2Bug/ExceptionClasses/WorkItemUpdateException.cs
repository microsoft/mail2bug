using System;

namespace Mail2Bug.ExceptionClasses
{
    class WorkItemUpdateException : Exception
    {
        public WorkItemUpdateException(int id) : this(id, "") {}

        public WorkItemUpdateException(int id, string message) :
            base(string.Format("Error updating work item with id #{0}. {1}", id, message)) {}
    }
}
