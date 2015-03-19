using System;
using System.Linq;
using log4net;
using Mail2Bug.Email;
using Mail2Bug.MessageProcessingStrategies;
using Mail2Bug.WorkItemManagement;

namespace Mail2Bug
{
	class Mail2BugEngine 
	{
	    private readonly IMailboxManager _mailboxManager;
		private readonly IMessageProcessingStrategy _messageProcessingStrategy;
		private readonly Config.InstanceConfig _config;

		public Mail2BugEngine(Config.InstanceConfig configInstance)
		{
		    _config = configInstance;

		    Logger.InfoFormat("Initalizing MailboxManager");
            _mailboxManager = MailboxManagerFactory.CreateMailboxManager(_config.EmailSettings);

		    Logger.InfoFormat("Initializing WorkItemManager");
            IWorkItemManager workItemManager;
            if (configInstance.TfsServerConfig.SimulationMode)
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
		    _messageProcessingStrategy = new SimpleBugStrategy(_config, workItemManager);
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
			Logger.InfoFormat("Reading messages from inbox ({0})", _config.EmailSettings.IncomingFolder);

            // Retreive the messages from the relevant mail folder
			var inboxItemsList = _mailboxManager.ReadMessages().ToList();

            if (inboxItemsList.Count == 0)
            {
                Logger.InfoFormat("No messages found in folder {0} (Instance {1})", _config.EmailSettings.IncomingFolder, _config.Name);
                return;
            }

            Logger.InfoFormat("Found {0} messages in folder {1} (Instance {2}). Processing...", 
                inboxItemsList.Count, _config.EmailSettings.IncomingFolder, _config.Name);

            foreach (var message in inboxItemsList)
			{
			    try
			    {
			        Logger.InfoFormat("Processing message {0}", message.Subject);
			        Logger.DebugFormat("Message sent on {0}", message.SentOn.ToLocalTime());
			        _messageProcessingStrategy.ProcessInboxMessage(message);
			        Logger.InfoFormat("Message '{0}' processed successfully, moving to next message", message.Subject);
			    }
			    catch (Exception exception)
			    {
			        Logger.Error("Error processing message", exception);
			    }
			    finally
			    {
                    message.Delete();
			    }
			}
		}

        private static readonly ILog Logger = LogManager.GetLogger(typeof(Mail2BugEngine));
    }
}
