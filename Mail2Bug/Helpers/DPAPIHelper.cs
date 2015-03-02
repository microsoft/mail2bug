using System.IO;
using System.Security.Cryptography;
using System.Text;
using log4net;

namespace Mail2Bug.Helpers
{
    public class DPAPIHelper
    {
        public static void WriteDataToFile(string data, string filename)
        {
            var serializedData = Encoding.UTF8.GetBytes(data);

            Logger.InfoFormat("Encrypting data using DPAPI");
            var encryptedData = ProtectedData.Protect(serializedData, null, DataProtectionScope.CurrentUser);
            Logger.InfoFormat("Data encrypted successfully. Creating file {0}", filename);

            using (var fs = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite))
            {
                Logger.InfoFormat("Created file {0}. Writing data", filename);
                fs.Write(encryptedData, 0, encryptedData.Length);
                Logger.InfoFormat("Data written successfully. Closing file");
            }
        }

        public static string ReadDataFromFile(string filename)
        {
            Logger.InfoFormat("Reading encrypted data from file {0}", filename);
            using (var fs = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite))
            {
                var encryptedData = new byte[fs.Length];
                fs.Read(encryptedData, 0, encryptedData.Length);
                var serializedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
                var data = Encoding.UTF8.GetString(serializedData);

                return data;
            }
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof (DPAPIHelper));
    }
}
