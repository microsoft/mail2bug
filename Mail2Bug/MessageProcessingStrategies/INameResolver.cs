namespace Mail2Bug.MessageProcessingStrategies
{
    public interface INameResolver
    {
        string Resolve(string alias, string name);
    }
}