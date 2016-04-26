using System;
using System.Collections.Generic;
using System.Linq;
using log4net;

namespace Mail2Bug.MessageProcessingStrategies
{
    /// <summary>
    /// Resolves values based on the current date.
    /// This is used for values that change based on the date like iteration paths.
    /// By supplying a list of entries - each value with the start date of when we should start to use it, we then resolve based on
    /// the current date by finding the entry with the most recent start date.
    /// 
    /// A default value is provided for the case when the current date is before any of the start dates.
    /// </summary>
    public class DateBasedValueResolver
    {
        readonly SortedList <DateTime, string> _valuesByStartDate;

        public DateBasedValueResolver(string defaultValue, SortedList<DateTime,string> iterationsByStartDate )
        {
            if (defaultValue == null) throw new ArgumentException("defaultValue can't be null", nameof(defaultValue));

            _valuesByStartDate = iterationsByStartDate ?? new SortedList<DateTime, string>();

            // Add the default value with the earliest start date possible
            _valuesByStartDate[DateTime.MinValue] = defaultValue;
        }

        public string Resolve(DateTime date)
        {
            var resolvedEntry = _valuesByStartDate.Last(x => x.Key <= date);
            Logger.InfoFormat("Date {0} resolved to value {1} (start date: {2})", date, resolvedEntry.Value, resolvedEntry.Key);
            return resolvedEntry.Value;
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof (DateBasedValueResolver));
    }
}
