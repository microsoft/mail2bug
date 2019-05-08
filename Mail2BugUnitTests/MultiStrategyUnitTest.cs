using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mail2Bug.MessageProcessingStrategies;
using Mail2BugUnitTests.Mocks;
using Mail2BugUnitTests.Mocks.Email;

namespace Mail2BugUnitTests
{
    [TestClass]
    public class MultiStrategyUnitTest
    {
        [TestMethod]
        public void MultipleStrategiesCalled()
        {
            List<IMessageProcessingStrategy> mocks = new List<IMessageProcessingStrategy>();
            IncomingEmailMessageMock messageMock = new IncomingEmailMessageMock();

            mocks.Add(new ProcessingStrategyMock());
            mocks.Add(new ProcessingStrategyMock());
            MultiStrategy m = new MultiStrategy(mocks);

            m.ProcessInboxMessage(messageMock);

            foreach (ProcessingStrategyMock strategy in mocks) {
                Assert.IsTrue(strategy.status, "Process message not successfully called on child strategies.");
            }
        }
    }
}
