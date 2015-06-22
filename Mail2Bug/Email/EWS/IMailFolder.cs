using System.Collections.Generic;

namespace Mail2Bug.Email.EWS
{
    public interface IMailFolder
    {
        int GetTotalCount();
        IEnumerable<IIncomingEmailMessage> GetMessages();
    }
}