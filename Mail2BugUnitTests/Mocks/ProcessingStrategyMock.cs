using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mail2Bug.MessageProcessingStrategies;
using Mail2Bug.Email;

namespace Mail2BugUnitTests.Mocks
{
    public class ProcessingStrategyMock : IMessageProcessingStrategy
    {
        public bool status { get; set; }
        public ProcessingStrategyMock() {
            status = false;
        }
        public void ProcessInboxMessage(IIncomingEmailMessage message) {
            status = true;
        }
    }
}
