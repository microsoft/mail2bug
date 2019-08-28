namespace Mail2Bug.Email
{
    /// <summary>
    /// Represents basic information about an Exchange email attachment that has been downloaded locally and has a known exchange content id
    /// </summary>
    public class MessageAttachmentInfo
    {
        public MessageAttachmentInfo(string filePath, string contentId)
        {
            FilePath = filePath;
            ContentId = contentId;
        }

        public string FilePath { get; }

        public string ContentId { get; }
    }
}