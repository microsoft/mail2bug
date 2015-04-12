using System.Collections.Generic;

namespace Mail2Bug.Email
{
    public interface IMailboxManager
    {
        /// <summary>
        /// Retrieve all relevant messages
        /// </summary>
        /// <returns>The messages that should be processed</returns>
        IEnumerable<IIncomingEmailMessage> ReadMessages();

        /// <summary>
        /// This function is called for every message once it's been processsed, so that the mailbox
        /// manager can take the desired action (delete it, move it to a different folder, etc.)
        /// </summary>
        /// <param name="message">The message that was processed</param>
        /// <param name="successful">Whether the message was processed successfully or not</param>
        void OnProcessingFinished(IIncomingEmailMessage message, bool successful);
    }
}
