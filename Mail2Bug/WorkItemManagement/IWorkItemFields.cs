namespace Mail2Bug.WorkItemManagement
{
    /// <summary>
    /// Set of fields of a work item.
    /// </summary>
    public interface IWorkItemFields
    {
        /// <summary>
        /// ID of the work item.
        /// </summary>
        string ID { get; }

        /// <summary>
        /// Title of the work item.
        /// </summary>
        string Title { get; }

        /// <summary>
        /// State of the work item.
        /// </summary>
        string State { get; }

        /// <summary>
        /// Current owner of the work item.
        /// </summary>
        string Owner { get; }

        /// <summary>
        /// Get value for a specific work item field.
        /// </summary>
        /// <param name="fieldName">name of the field to get</param>
        /// <returns>value for the field</returns>
        string GetFieldValue(string fieldName);
    }
}
