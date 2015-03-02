using log4net;
using Mail2Bug.ExceptionClasses;
using Mail2Bug.WorkItemManagement;

namespace Mail2Bug.MessageProcessingStrategies
{
    public class MessageProcessingStrategyFactory
    {
        public static IMessageProcessingStrategy CreateProcessingStrategy(
            Config.InstanceConfig config, 
            IWorkItemManager workItemManager)
        {
            switch (config.WorkItemSettings.ProcessingStrategy)
            {
                case Config.WorkItemSettings.ProcessingStrategyType.SimpleBugStrategy:
                    Logger.InfoFormat("Using SimpleBugStrategy");
                    return new SimpleBugStrategy(config, workItemManager);
                case Config.WorkItemSettings.ProcessingStrategyType.UpdateItemMetadataStrategy:
                    Logger.InfoFormat("Using UpdateItemMetadataStrategy");
                    return new UpdateItemMetadataStrategy(config, workItemManager);
                default:
                    Logger.ErrorFormat("Invalid processing strategy provided {0}", config.WorkItemSettings.ProcessingStrategy);
                    throw new BadConfigException(
                        "ProcessingStrategy",
                        string.Format("Invalid processing strategy provided {0}", config.WorkItemSettings.ProcessingStrategy));
            }
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof (MessageProcessingStrategyFactory));
    }
}
