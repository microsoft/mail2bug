using System;
using System.Linq;
using log4net;
using Mail2Bug.Email;
using Mail2Bug.Helpers;
using Mail2Bug.MessageProcessingStrategies;
using Mail2Bug.WorkItemManagement;

namespace Mail2Bug
{
	class Mail2BugEngine : IDisposable
	{
	    private readonly IMailboxManager _mailboxManager;
		private readonly Config.InstanceConfig _config;

        // We're using lazy initilization for the message processing strategy because it involves
        // initializing the work-item cache, which can be time consuming. For servers that host a high
        // number of instances and are using TemporaryInstanceRunner, there's a high likelihood that for
        // each specific iteration, many of the instances would have no messages to process. In such cases
        // the initialization of the work-item cache is redundant and slows down the process.
        // 
        // For cases where we're using PersistentInstanceRunner, this is still an improvement, albeit a much
        // smaller one, since early intializations benefit future processing cycles.
        private readonly Lazy<IMessageProcessingStrategy> _messageProcessingStrategy;

		public Mail2BugEngine(Config.InstanceConfig configInstance, MailboxManagerFactory mailboxManagerFactory)
		{
		    _config = configInstance;

		    Logger.InfoFormat("Initalizing MailboxManager");
            _mailboxManager = mailboxManagerFactory.CreateMailboxManager(_config.EmailSettings);

		    Logger.InfoFormat("Initializing WorkItemManager");
            _messageProcessingStrategy = new Lazy<IMessageProcessingStrategy>(InitProcessingStrategy);
		}

	    public void ProcessInbox()
		{
			try
			{
                Logger.InfoFormat("Running for config instance '{0}'", _config.Name);
                ProcessInboxInternal();
			}
			catch (Exception exception)
			{
				Logger.ErrorFormat("Exception while processing inbox for instance {0}\n{1}", _config.Name, exception);
			}
		}

        /// <summary>
        /// This method is responsible for doing the actual work of processing the inbox.
        /// It retreives all the messages from the relevant folder, and sends them to the message processing strategy
        /// for handling.
        /// Handling of each message is done in an exception-safe way (within a try-catch), to ensure that exceptions
        /// in processing one message don't affect the remaining messages.
        /// </summary>
		private void ProcessInboxInternal()
		{
			Logger.DebugFormat("Reading messages from inbox ({0})", _config.EmailSettings.IncomingFolder);

            // Retreive the messages from the relevant mail folder
			var inboxItemsList = _mailboxManager.ReadMessages().ToList();

            if (inboxItemsList.Count == 0)
            {
                Logger.DebugFormat("No messages found for instance {0}", _config.Name);
                return;
            }

            Logger.InfoFormat("Found {0} messages for Instance {1}. Processing...", inboxItemsList.Count, _config.Name);

            foreach (var message in inboxItemsList)
            {
                var messageProcessedSuccessfully = true;
			    try
			    {
			        Logger.InfoFormat("Processing message {0}", message.Subject);
			        Logger.DebugFormat("Message sent on {0}", message.SentOn.ToLocalTime());
			        _messageProcessingStrategy.Value.ProcessInboxMessage(message);
			        Logger.InfoFormat("Message '{0}' processed successfully, moving to next message", message.Subject);
			    }
			    catch (Exception exception)
			    {
			        messageProcessedSuccessfully = false;
			        Logger.Error("Error processing message", exception);
			    }
			    finally
			    {
			        _mailboxManager.OnProcessingFinished(message, messageProcessedSuccessfully);
			    }
			}
		}

        private IMessageProcessingStrategy InitProcessingStrategy()
        {
            IWorkItemManager workItemManager;
            if (_config.TfsServerConfig.SimulationMode)
            {
                Logger.InfoFormat("Working in simulation mode. Using WorkItemManagerMock");
                workItemManager = new WorkItemManagerMock(_config.WorkItemSettings.ConversationIndexFieldName);
            }
            else
            {
                Logger.InfoFormat("Working in standard mode, using TFSWorkItemManager");
                workItemManager = new TFSWorkItemManager(_config);
            }

            Logger.InfoFormat("Initializing MessageProcessingStrategy");
            return new SimpleBugStrategy(_config, workItemManager);
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(Mail2BugEngine));
	    public void Dispose()
	    {
	        DisposeUtils.DisposeIfDisposable(_messageProcessingStrategy);
	    }
	}
}
