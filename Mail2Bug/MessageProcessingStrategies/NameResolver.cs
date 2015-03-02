using System.Collections.Generic;
using System.Linq;
using log4net;

namespace Mail2Bug.MessageProcessingStrategies
{
	// This class is intended to be used for resolving the proper name value to use in TFS fields (e.g. 'Assigned to')
	// based on an alias.
	public class NameResolver : INameResolver
	{
		private readonly HashSet<string> _namesList;

		// The constructor takes the list of allowed values
		public NameResolver(IEnumerable<string> namesList)
		{
		    var normalizedName = from name in namesList select Normalize(name);
		    _namesList = new HashSet<string>(normalizedName);
		}

	    public string Resolve(string alias, string name)
        {
            Logger.InfoFormat("Resolving name for alias/name {0}/{1}", alias, name);

            if (IsValidName(name))
            {
                Logger.DebugFormat("Found name '{0}', returning", name);
                return name;
            }

            if (IsValidName(alias))
            {
                Logger.DebugFormat("Found alias '{0}', returning", name);
                return alias;
            }

	        return null;
        }

        private bool IsValidName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Logger.WarnFormat("Trying to resolve a null/empty name");
                return false;
            }

            return _namesList.Contains(Normalize(name));
        }
    
        private static string Normalize(string name)
        {
            return name.ToLower();
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(NameResolver));
    }
}
