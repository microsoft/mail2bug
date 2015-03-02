namespace Mail2Bug.Email
{
    public interface IMailSender
    {
        void SendMessage(string to, string cc, string bcc, string subject, string body);
    }
}
