namespace Mail2Bug.Email
{
    /// <summary>
    /// Similar to IIncomingEmailMessage, this interface is an adapter for attachments of incoming email messages.
    /// </summary>
    public interface IIncomingEmailAttachment
    {
        string SaveAttachmentToFile();
        string SaveAttachmentToFile(string filename);
        string ContentId { get; set; }
    }
}
