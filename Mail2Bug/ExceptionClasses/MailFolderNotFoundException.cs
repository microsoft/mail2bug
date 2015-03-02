using System;

namespace Mail2Bug.ExceptionClasses
{
    class MailFolderNotFoundException : Exception
    {
        public MailFolderNotFoundException(string folderName) : base(string.Format("Couldn't find mail folder '{0}'", folderName)) {}
    }
}
