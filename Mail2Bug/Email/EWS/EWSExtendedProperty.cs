using Microsoft.Exchange.WebServices.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mail2Bug.Email.EWS
{
    public class EWSExtendedProperty
    {
        private const int PidTagBodyIdentifier = 0x1000;
        private const int PidTagConversationIdIdentifier = 0x3013;

        // Extended property for PidTagBody, which is the message body converted to plain text format
        // See https://msdn.microsoft.com/en-us/library/ee158918%28EXCHG.80%29.aspx
        public static readonly ExtendedPropertyDefinition PidTagBody = new ExtendedPropertyDefinition(PidTagBodyIdentifier, MapiPropertyType.String);

        // Extended property for PidTagConversationId, which is the GUID portion of the ConversationIndex
        // See https://msdn.microsoft.com/en-us/library/cc433490(v=EXCHG.80).aspx and
        // https://msdn.microsoft.com/en-us/library/ee204279(v=exchg.80).aspx for more information
        public static readonly ExtendedPropertyDefinition PidTagConversationId = new ExtendedPropertyDefinition(PidTagConversationIdIdentifier, MapiPropertyType.Binary);

    }
}
