using System.Collections.Generic;

namespace Mail2Bug.MessageProcessingStrategies
{
    public class NameResolverMock : INameResolver
    {
        public string Resolve(string alias, string name)
        {
            var resolution = TryResolveAlias(alias);
            return resolution ?? name;
        }

        private string TryResolveAlias(string alias)
        {
            return AliasToNameMap.ContainsKey(alias) ? AliasToNameMap[alias] : null;
        }

        public Dictionary<string, string> AliasToNameMap = new Dictionary<string, string>();
    }

    public class IdentityFunctionNameResolverMock : INameResolver
    {
        public string Resolve(string alias, string name)
        {
            return name;
        }
    }
}
