using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Mail2Bug.Helpers
{
    public class FileUtils
    {
        private const int MaxTries = 10;

        public static string GetValidFileName(string baseFilename, string extension, string path)
        {
            path = string.IsNullOrEmpty(path) ? @".\" : path;
            baseFilename = string.IsNullOrEmpty(baseFilename) ? GetRandomFilename() : baseFilename;

            baseFilename = ReplaceInvalidChars(baseFilename);
            extension = extension.StartsWith(".") ? extension.Substring(1) : extension;

            for (var counter = 0; counter < MaxTries; ++counter)
            {
                var fullPath = Path.Combine(path, ComposeFileName(baseFilename, extension, counter));
                if (!File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return Path.Combine(
                path, 
                string.Format("{0}{1}.{2}", 
                    baseFilename, 
                    Rand.Next().ToString(CultureInfo.InvariantCulture),
                    extension));
        }

        private static string GetRandomFilename()
        {
            return "Unnamed" + Rand.Next().ToString(CultureInfo.InvariantCulture);
        }

        private static string ComposeFileName(string baseFilename, string extension, int counter)
        {
            var differentiator = new string('_', counter);
            return string.Format("{0}{1}.{2}", baseFilename, differentiator, extension);
        }

        public static string ReplaceInvalidChars(string filename)
        {
            var illegalChars = Path.GetInvalidFileNameChars();
            illegalChars.ToList().ForEach(c => filename = filename.Replace(c, '_'));

            return filename;
        }

        private static readonly Random Rand = new Random();
    }
}
