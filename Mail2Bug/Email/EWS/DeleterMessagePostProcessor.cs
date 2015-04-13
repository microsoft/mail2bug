namespace Mail2Bug.Email.EWS
{
    class DeleterMessagePostProcessor : IMessagePostProcessor
    {
        public void Process(EWSIncomingMessage message, bool successful)
        {
            message.Delete();
        }
    }
}
