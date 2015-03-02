using Mail2Bug.Email;

namespace Mail2Bug.MessageProcessingStrategies
{
    public interface IMessageProcessingStrategy
    {
        void ProcessInboxMessage(IIncomingEmailMessage message);
    }
}
