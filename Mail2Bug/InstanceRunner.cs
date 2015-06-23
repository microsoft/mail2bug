using System;
using Mail2Bug.Email;

namespace Mail2Bug
{
    interface IInstanceRunner
    {
        string Name { get; }
        void RunInstance();
    }

    // This implementation initializes the instance during construction, and keeps the initialized
    // objects, so that you don't need to re-run the initialization every time the instance is run.
    // This strategy reduces the need to run the initialization many times, and is the recommended 
    // one for servers running a small number of instances
    class PersistentInstanceRunner : IInstanceRunner, IDisposable
    {
        public PersistentInstanceRunner(Config.InstanceConfig instanceConfig, MailboxManagerFactory mailboxManagerFactory)
        {
            Name = instanceConfig.Name;
            _engine = new Mail2BugEngine(instanceConfig, mailboxManagerFactory);
        }

        public string Name { get; private set; }

        public void RunInstance()
        {
            _engine.ProcessInbox();
        }

        public void Dispose()
        {
            _engine.Dispose();
        }

        private readonly Mail2BugEngine _engine;
    }

    // This implementation initializes the instance every time it is run, then disposes of the objects
    // and has to reinitialize them on the next run. While this is less performant, it maintains a smaller
    // memory footprint and is recommended for servers running a lot of instances (40 or more), because
    // the TFS clients start to have issues once too many of these clients are in existence at the same
    // time (usually characterized by COM exceptions)
    class TemporaryInstanceRunner : IInstanceRunner
    {
        public TemporaryInstanceRunner(Config.InstanceConfig instanceConfig, MailboxManagerFactory mailboxManagerFactory)
        {
            _instanceConfig = instanceConfig;
            _mailboxManagerFactory = mailboxManagerFactory;
            Name = instanceConfig.Name;
        }

        public string Name { get; private set; }

        public void RunInstance()
        {
            using (var engine = new Mail2BugEngine(_instanceConfig, _mailboxManagerFactory))
            {
                engine.ProcessInbox();
            }
        }

        private readonly Config.InstanceConfig _instanceConfig;
        private readonly MailboxManagerFactory _mailboxManagerFactory;
    }
}
