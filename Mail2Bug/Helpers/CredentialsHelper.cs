using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using log4net;
using System;
using Microsoft.Azure.KeyVault;
using System.Threading.Tasks;
using Mail2Bug.ExceptionClasses;

namespace Mail2Bug.Helpers
{
    public class CredentialsHelper  
    {
        private string clientId { get; set; }
        private string clientSecret { get; set; }

        public string GetPassword(string dpapiFilePath, System.Security.Cryptography.DataProtectionScope scope, Config.KeyVaultSecret secretsTuple)
        {
            var task = GetPasswordAsync(dpapiFilePath, scope, secretsTuple);

            task.Wait();
            if (task.IsFaulted)
            {
                throw task.Exception;
            }
            return task.Result;
        }

        public async Task<string> GetPasswordAsync(string dpapiFilePath, System.Security.Cryptography.DataProtectionScope scope, Config.KeyVaultSecret secretsTuple)
        {
            if (secretsTuple == null)
            {
                if (string.IsNullOrWhiteSpace(dpapiFilePath))
                {
                    return null;
                }

                if (!File.Exists(dpapiFilePath)) 
                {
                    throw new BadConfigException("Protected file missing", dpapiFilePath);
                }

                return DPAPIHelper.ReadDataFromFile(dpapiFilePath, scope);
            }

            if (string.IsNullOrWhiteSpace(secretsTuple.ApplicationIdEnvironmentVariableName))
            {
                throw new BadConfigException("Application ID", "Empty Application ID variable name");
            }

            if (string.IsNullOrWhiteSpace(secretsTuple.ApplicationSecretEnvironmentVariableName)) 
            {
                throw new BadConfigException("Application Secret", "Empty Application Secret variable name");
            }

            clientId = GetRequiredEnvironmentVariable(secretsTuple.ApplicationIdEnvironmentVariableName);
            clientSecret = GetRequiredEnvironmentVariable(secretsTuple.ApplicationSecretEnvironmentVariableName);

            var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetToken));
            var result = await kv.GetSecretAsync(secretsTuple.KeyVaultPath);
            return result.Value;
        }

        private static string GetRequiredEnvironmentVariable(string variableName)
        {
            string value = Environment.GetEnvironmentVariable(variableName);
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(variableName);
            }

            return value;
        }

        public async Task<string> GetToken(string authority, string resource, string scope)
        {
            var authContext = new AuthenticationContext(authority);
            ClientCredential clientCred = new ClientCredential(clientId, clientSecret);
            AuthenticationResult result = await authContext.AcquireTokenAsync(resource, clientCred);

            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }

            return result.AccessToken;
        }
    }
}
