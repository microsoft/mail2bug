using System.IO;
using System.Security.Cryptography;
using System.Text;
using log4net;

namespace Mail2Bug.Helpers
{
    public class DPAPIHelper
    {
        /// <summary>
        /// Writes string data to target file name using specified data protection scope
        /// </summary>
        /// <param name="data">string to encrypt</param>
        /// <param name="filename">filename to write out to</param>
        /// <param name="scope">scope of protection to use (user or machine)</param>
        public static void WriteDataToFile(string data, string filename, DataProtectionScope scope)
        {
            Logger.InfoFormat("Encrypting data using DPAPI");
            var encryptedData = Encrypt(data, null, scope);
            Logger.InfoFormat("Data encrypted successfully. Creating file {0}", filename);

            using (var fs = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite))
            {
                Logger.InfoFormat("Created file {0}. Writing data", filename);
                fs.Write(encryptedData, 0, encryptedData.Length);
                Logger.InfoFormat("Data written successfully. Closing file");
            }
        }

        /// <summary>
        /// Reads encrypted string data from source filename that was encrypted with specified scope scheme
        /// </summary>
        /// <param name="filename">Path to source file</param>
        /// <param name="scope">scope of protection of souce file</param>
        /// <returns></returns>
        public static string ReadDataFromFile(string filename, DataProtectionScope scope)
        {
            Logger.InfoFormat("Reading encrypted data from file {0}", filename);
            using (var fs = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite))
            {
                var encryptedData = new byte[fs.Length];
                fs.Read(encryptedData, 0, encryptedData.Length);
                var data = Decrypt(encryptedData, null, scope);

                return data;
            }
        }


        /// <summary>
        /// Assumes UTF8 and encodes using DPAPI
        /// </summary>
        /// <param name="stringToEncrypt">string to encrypt</param>
        /// <param name="optionalEntropy">optional entropy to include</param>
        /// <param name="scope">scope of protection</param>
        /// <returns></returns>
        public static byte[] Encrypt(string stringToEncrypt, byte[] optionalEntropy, DataProtectionScope scope)
        {
            return ProtectedData.Protect(Encoding.UTF8.GetBytes(stringToEncrypt), optionalEntropy, scope);
        }


        /// <summary>
        /// Assumes UTF8 and decodes using DPAPI
        /// </summary>
        /// <param name="encryptedData">encrypted data</param>
        /// <param name="optionalEntropy">optional entropy that was used during encryption</param>
        /// <param name="scope">scope of protection</param>
        /// <returns></returns>
        public static string Decrypt(byte[] encryptedData, byte[] optionalEntropy, DataProtectionScope scope)
        {
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(encryptedData, optionalEntropy, scope));
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(DPAPIHelper));
    }
}
