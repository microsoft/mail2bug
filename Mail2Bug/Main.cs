using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;

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

                var iterations = ReadIntFromAppConfig("Iterations");
                var interval = TimeSpan.FromSeconds(ReadIntFromAppConfig("IntervalInSeconds"));
                var useThreads = ReadBoolFromAppConfig("UseThreads");

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
            if (!useThreads)
            {
                RunInstancesSingleThreaded();
                return;
            }

            RunInstancesMultithreaded();
        }

        private static void RunInstancesSingleThreaded()
        {
            var task = new Task(() => _instances.ForEach(x => x.ProcessInbox()));
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

            _instances.ForEach(x => tasks.Add(new Task(x.ProcessInbox)));
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
            _instances = new List<Mail2BugEngine>();
            foreach (var config in configs)
            {
                foreach (var instance in config.Instances)
                {
                    InitSingleInstance(instance);
                }
            }
        }

        private static void InitSingleInstance(Config.InstanceConfig instance)
        {
            try
            {
                Logger.InfoFormat("Initializing engine for instance '{0}'", instance.Name);
                _instances.Add(new Mail2BugEngine(instance));
                Logger.InfoFormat("Finished initialization of engine for instance '{0}'", instance.Name);
            }
            catch(Exception ex)
            {
                Logger.ErrorFormat("Exception while initializing instance '{0}'\n{1}", instance.Name, ex);
            }
        }

        private static int ReadIntFromAppConfig(string setting)
        {
            return int.Parse(ConfigurationManager.AppSettings[setting]);
        }

        private static bool ReadBoolFromAppConfig(string setting)
        {
            return bool.Parse(ConfigurationManager.AppSettings[setting]);
        }

        private static List<Mail2BugEngine> _instances;
        private static TimeSpan _timeoutPerIteration = TimeSpan.FromMinutes(30);

        private static readonly ILog Logger = LogManager.GetLogger(typeof (MainApp));
    }
}
