namespace Mail2Bug.Email.EWS
{
    public interface IMessagePostProcessor
    {
        void Process(EWSIncomingMessage message, bool successful);
    }
}
