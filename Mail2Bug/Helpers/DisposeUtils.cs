using System;

namespace Mail2Bug.Helpers
{
    public class DisposeUtils
    {
        public static void DisposeIfDisposable(object obj)
        {
            var disposable = obj as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }
    }
}
