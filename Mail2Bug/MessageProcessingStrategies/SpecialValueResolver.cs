using System;
using System.Collections.Generic;
using System.Globalization;
using log4net;
using Mail2Bug.Email;
using Mail2Bug.Helpers;

namespace Mail2Bug.MessageProcessingStrategies
{
    public class SpecialValueResolver
    {
        #region Definitions and Constants

        public const string SubjectKeyword = "##Subject";
        public const string SenderKeyword = "##Sender";
        public const string MessageBodyKeyword = "##MessageBody";
        public const string MessageBodyWithSenderKeyword = "##MessageBodyWithSender";
        public const string RawMessageBodyKeyword = "##RawMessageBody";
        public const string NowKeyword = "##Now";
        public const string TodayKeyword = "##Today";
        public const string LocationKeyword = "##Location";
        public const string StartTimeKeyword = "##StartTime";
        public const string EndTimeKeyword = "##EndTime";

        #endregion

        public SpecialValueResolver(IIncomingEmailMessage message, INameResolver resolver)
        {
            _resolver = resolver;
            _valueResolutionMap = new Dictionary<string, string>();
            _valueResolutionMap[SubjectKeyword] = GetValidSubject(message);
            _valueResolutionMap[SenderKeyword] = GetSender(message);
            _valueResolutionMap[MessageBodyKeyword] = TextUtils.FixLineBreaks(message.PlainTextBody);
            _valueResolutionMap[MessageBodyWithSenderKeyword] =
                String.Format("{0}\n\nCreated by: {1} ({2})", 
                _valueResolutionMap[MessageBodyKeyword], 
                message.SenderName, 
                message.SenderAddress);
            _valueResolutionMap[RawMessageBodyKeyword] = TextUtils.FixLineBreaks(message.RawBody);
            _valueResolutionMap[NowKeyword] = DateTime.Now.ToString("g");
            _valueResolutionMap[TodayKeyword] = DateTime.Now.ToString("d");
            _valueResolutionMap[LocationKeyword] = message.Location;
            _valueResolutionMap[StartTimeKeyword] = GetValidTimeString(message.StartTime);
            _valueResolutionMap[EndTimeKeyword] = GetValidTimeString(message.EndTime);
        }


        /// Gets a keyword and returns its associated value.
        /// If the keyword doesn't exist, return the original value
        /// Note that keywords are *case sensitive*
        public string Resolve(string value)
        {
            if (!_valueResolutionMap.ContainsKey(value))
            {
                return value;
            }

            // Found specia value - return resolution
            var resolvedValue = _valueResolutionMap[value];
            Logger.DebugFormat("Resolved value '{0}' to '{1}'", value, resolvedValue);
            return resolvedValue;
        }

        // "Easy-access" properties to get specific values
        public string Subject { get { return _valueResolutionMap[SubjectKeyword]; } }
        public string Sender { get { return _valueResolutionMap[SenderKeyword]; } }
        public string MessageBody { get { return _valueResolutionMap[MessageBodyKeyword]; } }
        public string RawMessageBody { get { return _valueResolutionMap[RawMessageBodyKeyword]; } }
        public string Location { get { return _valueResolutionMap[LocationKeyword]; } }
        public string StartTime { get { return _valueResolutionMap[StartTimeKeyword]; } }
        public string EndTime { get { return _valueResolutionMap[EndTimeKeyword]; } }

        private string GetSender(IIncomingEmailMessage message)
        {
            var resolvedName = _resolver.Resolve(message.SenderAlias, message.SenderName);

            if (!string.IsNullOrEmpty(resolvedName))
            {
                Logger.InfoFormat("Alias '{0}', resolved to {1}", message.SenderAlias, resolvedName);
                return resolvedName;
            }

            // Name resolution was not successful, return the display name
            Logger.WarnFormat("Name resolution failed for sender alias '{0}'. Returning display name.",
                                message.SenderAlias);
            return message.SenderName;
        }

        private static string GetValidSubject(IIncomingEmailMessage message)
        {
            return !string.IsNullOrEmpty(message.ConversationTopic) ? message.ConversationTopic : "NO SUBJECT";
        }

        private string GetValidTimeString(DateTime? dateTime)
        {
            if (dateTime.HasValue)
            {
                return dateTime.Value.ToString(CultureInfo.InvariantCulture);
            }

            return "No time specified (probably this is not a meeting request)";
        }

        private readonly INameResolver _resolver;
        private readonly Dictionary<string, string> _valueResolutionMap;

        private static readonly ILog Logger = LogManager.GetLogger(typeof(SpecialValueResolver));
    }
}
