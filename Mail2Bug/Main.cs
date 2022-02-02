using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Mail2Bug.Email;
using Mail2Bug.Email.EWS;

[assembly: CLSCompliant(false)]

namespace Mail2Bug
{
    class MainApp
    {
        /// <summary>
        /// The main entry point for the windows application.
        /// </summary>
        [STAThread]
        public static void Main(string[] args) // string[] args
        {
            if(args.Contains("-break"))
            {
                Logger.Info("Breaking into debugger");
                Debugger.Break();
            }

            try
            {
                // Enforce TLS 1.2
                if (ReadBoolFromAppConfig("EnforceTls12", false))
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                }

                string configPath = ConfigurationManager.AppSettings["ConfigPath"];
                string configsFilePattern = ConfigurationManager.AppSettings["ConfigFilePattern"];

                var configFiles = Directory.GetFiles(configPath, configsFilePattern);
                if (configFiles.Length == 0)
                {
                    Logger.ErrorFormat("No configs found (path='{0}', pattern='{1}')", configPath, configsFilePattern);
                    throw new ConfigurationErrorsException("No configs found");
                }

                var configs = new List<Config>();
                var configTimeStamps = new Dictionary<string, DateTime>();

                foreach (var configFile in configFiles)
                {
                    // Save the timestamp for the config so that we can detect if it changed later on
                    configTimeStamps[configFile] = File.GetLastWriteTime(configFile);

                    // Load the config and add it to the list.
                    // If loading failed, print error message and continue
                    var cfg = TryLoadConfig(configFile);
                    if (cfg == null)
                    {
                        Logger.ErrorFormat("Couldn't load config file {0}. Skipping that config file.", configFile);
                        continue;
                    }

                    configs.Add(cfg);
                }

                if (configs.Count == 0)
                {
                    throw new ConfigurationErrorsException("None of the configs were valid");
                }

                InitInstances(configs);

                var iterations = ReadIntFromAppConfig("Iterations", 200);
                var interval = TimeSpan.FromSeconds(ReadIntFromAppConfig("IntervalInSeconds", 1));
                var useThreads = ReadBoolFromAppConfig("UseThreads", false);

                for (var i = 0; i < iterations; ++i )
                {
                    Logger.InfoFormat("{0} Iteration {1} {0}", new string('-', 15), i);
                    RunInstances(useThreads);

                    if (IsConfigsChanged(configTimeStamps))
                    {
                        break;
                    }

                    Thread.CurrentThread.Join(interval); // Sleep between iterations
                }

                foreach (var instance in _instances)
                {
                    var disposable = instance as IDisposable;
                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.ErrorFormat("Exception caught in main - aborting. {0}", exception);
            }
        }

        private static Config TryLoadConfig(string configFile)
        {
            try
            {
                return Config.GetConfig(configFile);
            }
            catch(Exception ex)
            {
                Logger.ErrorFormat("Exception when trying to load config from file {0}\n{1}", configFile, ex);
            }

            return null;
        }

        private static bool IsConfigsChanged(Dictionary<string, DateTime> configTimeStamps)
        {
            foreach (var timeStampEntry in configTimeStamps)
            {
                if (timeStampEntry.Value != File.GetLastWriteTime(timeStampEntry.Key))
                {
                    Logger.InfoFormat("Config '{0}' changed. Breaking.", timeStampEntry.Key);
                    return true;
                }
            }

            return false;
        }

        private static void RunInstances(bool useThreads)
        {
            // At the beginning of each iteration, update the inboxes of EWS connections - specifically
            // for instances relying on RecipientsMailboxManagerRouter.
            // We cannot make the calls to process inboxes implicit in RecipientMailboxManagerRouter.GetMessages
            // because then it would be called by each instance, which is exactly what we want to avoid.
            // The alternatives are:
            // * Expose a function to process inboxes and call it at the beginning of each iteration (which is the
            //   solution implemented her)
            // * Add logic to RecipientsMailboxManagerRouter to detect wheter a call to ProcessInbox is needed or 
            //   not based on whether new messages were received or similar logic. I initially implemented this logic
            //   but then decided it's adding too much complexity compared to the small benefit of a somewhat cleaner
            //   abstraction.
            //   If we decide otherwise in the future, we can simply add it in RecipientsMailboxManagerRouter and then
            //   get rid of the ProcessInboxes public method and the call to it here.
            _ewsConnectionManger.ProcessInboxes();

            if (!useThreads)
            {
                RunInstancesSingleThreaded();
                return;
            }

            RunInstancesMultithreaded();
        }

        private static void RunInstancesSingleThreaded()
        {
            var task = new Task(() => _instances.ForEach(x => x.RunInstance()));
            task.Start();
            bool done = task.Wait(_timeoutPerIteration);

            if (!done)
            {
                throw new TimeoutException(string.Format(
                    "Running instances took more than {0} minutes", _timeoutPerIteration.TotalMinutes));
            }
        }

        private static void RunInstancesMultithreaded()
        {
            // Multi-threaded invocation - dispatch each instance to run on a thread and wait for all threads to finish
            var tasks = new List<Task>();

            var sw = new Stopwatch();
            sw.Start();

            _instances.ForEach(x => tasks.Add(new Task(x.RunInstance)));
            tasks.ForEach(x => x.Start());
            tasks.ForEach(x => x.Wait(GetRemainigTimeout(sw, _timeoutPerIteration)));

            foreach (var task in tasks)
            {
                if (!task.IsCompleted)
                {
                    throw new TimeoutException(string.Format(
                        "Running instances took more than {0} minutes", _timeoutPerIteration.TotalMinutes));
                }
            }
        }

        private static TimeSpan GetRemainigTimeout(Stopwatch sw, TimeSpan totalTimeout)
        {
            var remainigTimeout = totalTimeout - sw.Elapsed;
            return remainigTimeout.CompareTo(TimeSpan.Zero) > 0 ? remainigTimeout : TimeSpan.Zero;
        }

        private static void InitInstances(IEnumerable<Config> configs)
        {
            _instances = new List<IInstanceRunner>();
            _ewsConnectionManger = new EWSConnectionManger(true);
            var mailboxManagerFactory = new MailboxManagerFactory(_ewsConnectionManger);

            foreach (var config in configs)
            {
                foreach (var instance in config.Instances)
                {
                    try
                    {
                        var usePersistentInstances = ReadBoolFromAppConfig("UsePersistentInstances", true);
                        Logger.InfoFormat("Initializing engine for instance '{0}' (Persistent? {1})", instance.Name, usePersistentInstances);
                        
                        if (usePersistentInstances)
                        {
                            _instances.Add(new PersistentInstanceRunner(instance, mailboxManagerFactory));
                        }
                        else
                        {
                            _instances.Add(new TemporaryInstanceRunner(instance, mailboxManagerFactory));
                        }

                        Logger.InfoFormat("Finished initialization of engine for instance '{0}'", instance.Name);
                    }
                    catch (Exception ex)
                    {
                        Logger.ErrorFormat("Exception while initializing instance '{0}'\n{1}", instance.Name, ex);
                    }
                }
            }
        }

        private static int ReadIntFromAppConfig(string setting, int defaultValue)
        {
            var value = ConfigurationManager.AppSettings[setting];
            return string.IsNullOrEmpty(value) ? defaultValue : int.Parse(value);
        }

        private static bool ReadBoolFromAppConfig(string setting, bool defaultValue)
        {
            var value = ConfigurationManager.AppSettings[setting];
            return string.IsNullOrEmpty(value) ? defaultValue : bool.Parse(value);
        }

        private static List<IInstanceRunner> _instances;
        private static TimeSpan _timeoutPerIteration = TimeSpan.FromMinutes(30);

        private static readonly ILog Logger = LogManager.GetLogger(typeof (MainApp));
        private static EWSConnectionManger _ewsConnectionManger;
    }
}
