namespace Mail2Bug.Email
{
    /// <summary>
    /// Similar to IIncomingEmailMessage, this interface is an adapter for attachments of incoming email messages.
    /// </summary>
    public interface IIncomingEmailAttachment
    {
        bool IsInline { get; }
        string ContentId { get; }
        string SaveAttachmentToFile();
        string SaveAttachmentToFile(string filename);
    }
}
