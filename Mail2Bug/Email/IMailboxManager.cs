using System.Collections.Generic;

namespace Mail2Bug.Email
{
    public interface IMailboxManager
    {
        /// <summary>
        /// Retrieve all relevant messages
        /// </summary>
        /// <returns></returns>
        IEnumerable<IIncomingEmailMessage> ReadMessages();
    }
}
