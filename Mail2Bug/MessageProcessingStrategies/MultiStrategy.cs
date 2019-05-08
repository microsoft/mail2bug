using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using log4net;
using Mail2Bug.Email;
using Mail2Bug.Helpers;
using Mail2Bug.WorkItemManagement;

namespace Mail2Bug.MessageProcessingStrategies
{
    /// <summary>
    /// Allows Mail2Bug administrators to create their own processing strategies to be run in conjunction with (or without) the default 
    /// SimpleBugStrategy. This opens up the door for plugins to write messages to storage or auto response bots while maintaining 
    /// current functionality.
    /// </summary>
    public class MultiStrategy : IMessageProcessingStrategy
    {
        private List<IMessageProcessingStrategy> _strategies;
        public MultiStrategy(List<IMessageProcessingStrategy> strategies) {
            _strategies = strategies;
        }
        public void ProcessInboxMessage(IIncomingEmailMessage message)
        {
            foreach (IMessageProcessingStrategy strategy in _strategies) {
                strategy.ProcessInboxMessage(message);
            }
        }
    }
}
