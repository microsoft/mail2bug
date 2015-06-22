using System.Collections.Generic;
using System.Linq;
using log4net;

namespace Mail2Bug.Email.EWS
{
    public class RecipientsMailboxManagerRouter
    {
        public delegate bool MessageEvaluator(IIncomingEmailMessage message);

        public RecipientsMailboxManagerRouter(IMailFolder folder)
        {
            _folder = folder;
        }

        public int RegisterMailbox(MessageEvaluator evaluator)
        {
            var id = _nextId++;
            _clients[id] = new ClientData
            {
                Messages = new List<IIncomingEmailMessage>(),
                Evaluator = evaluator
            };

            return id;
        }

        public IEnumerable<IIncomingEmailMessage> GetMessages(int clientId)
        {
            if (!_clients.ContainsKey(clientId))
            {
                Logger.ErrorFormat("Can't retrieve messages for client ID {0}. No such client registered",
                    clientId);

                return new List<IIncomingEmailMessage>();
            }

            return _clients[clientId].Messages;
        }

        public void ProcessInbox()
        {
            var messages = _folder.GetMessages();

            foreach (var clientData in _clients)
            {
                clientData.Value.Messages.Clear();
            }

            messages
                .OrderBy(message => message.ReceivedOn)
                .ToList()
                .ForEach(
                    message =>
                    {
                        foreach (var clientData in _clients)
                        {
                            if (clientData.Value.Evaluator(message))
                            {
                                Logger.InfoFormat(
                                    "Adding message to client queue: Client ID {0}; subject: {1}", 
                                    clientData.Key, 
                                    message.Subject);

                                clientData.Value.Messages.Add(message);
                                return;
                            }
                        }

                        Logger.InfoFormat("Message doesn't fit to any client. Subject: {0}", message.Subject);
                    });
            
        }

        private struct ClientData
        {
            public MessageEvaluator Evaluator { get; set; }
            public List<IIncomingEmailMessage> Messages { get; set; }
        }


        private readonly IMailFolder _folder;
        private readonly Dictionary<int, ClientData> _clients = new Dictionary<int, ClientData>();
        private int _nextId = 100;

        private static readonly ILog Logger = LogManager.GetLogger(typeof(RecipientsMailboxManagerRouter));
    }
}
