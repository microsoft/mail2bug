using System;
using System.Collections.Generic;
using log4net;
using Microsoft.Exchange.WebServices.Data;

namespace Mail2Bug.Email.EWS
{
    /// <summary>
    /// This class caches EWS connection objects based on their settings.
    /// When a caller asks for a new EWS connection, if an appropriate object already exists, we just return that object
    /// thus avoiding the long initialization time for EWS (~1 minute).
    /// 
    /// This works well for configurations where many InstanceConfigs are relying on the same user with different mail folders.
    /// 
    /// Since we may want to be able to turn off caching in some cases, the caching itself is controlled at initialization time.
    /// If caching is disabled, a new connection will be initiated for every call.
    /// </summary>
    public class EWSConnectionFactory
    {
        public struct Credentials
        {
            public string EmailAddress;
            public string UserName;
            public string Password;
        }

        public EWSConnectionFactory(bool enableConnectionCaching)
        {
            _enableConnectionCaching = enableConnectionCaching;

            if (_enableConnectionCaching)
            {
                _cachedConnections = new Dictionary<Tuple<string, string, int>, ExchangeService>();
            }
        }

        public ExchangeService GetConnection(Credentials credentials)
        {
            if (!_enableConnectionCaching)
            {
                return ConnectToEWS(credentials);
            }

            lock (_cachedConnections)
            {
                var key = GetKeyFromCredentials(credentials);

                if (_cachedConnections.ContainsKey(key))
                {
                    Logger.InfoFormat("FolderMailboxManager for {0} already exists - retrieving from cache", key);
                    return _cachedConnections[key];
                }

                Logger.InfoFormat("Creating FolderMailboxManager for {0}", key);
                _cachedConnections[key] = ConnectToEWS(credentials);
                return _cachedConnections[key];
            }
        }

        static private Tuple<string, string, int> GetKeyFromCredentials(Credentials credentials)
        {
            return new Tuple<string, string, int>(
                credentials.EmailAddress,
                credentials.UserName, credentials.Password.GetHashCode());
        }

        static private ExchangeService ConnectToEWS(Credentials credentials)
        {
            Logger.DebugFormat("Initializing FolderMailboxManager for email adderss {0}", credentials.EmailAddress);
            var connection = new ExchangeService(ExchangeVersion.Exchange2010_SP1)
            {
                Credentials = new WebCredentials(credentials.UserName, credentials.Password),
                Timeout = 60000
            };
            connection.AutodiscoverUrl(
                credentials.EmailAddress,
                x =>
                {
                    Logger.DebugFormat("Following redirection for EWS autodiscover: {0}", x);
                    return true;
                }
                );

            Logger.DebugFormat("Service URL: {0}", connection.Url);

            return connection;
        }


        private readonly Dictionary<Tuple<string, string, int>, ExchangeService> _cachedConnections;
        private readonly bool _enableConnectionCaching;

        private static readonly ILog Logger = LogManager.GetLogger(typeof(EWSConnectionFactory));
    }
}
