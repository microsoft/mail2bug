using System.Linq;
using log4net;
using Microsoft.Exchange.WebServices.Data;

namespace Mail2Bug.Email.EWS
{
    /// <summary>
    /// Utility class for resolving folder names/paths to actual Folder objects
    /// </summary>
    public class FolderNameResolver
    {
        // Resolve folder name to Folder object - the folder is expected to be immediately under the mailbox root
        public static Folder FindFolderByName(string folderName, ExchangeService service)
        {
            Logger.DebugFormat("Looking for folder named '{0}'", folderName);
            // Look for the folder under the mailbox root
            var rootFolder = Folder.Bind(service, WellKnownFolderName.MsgFolderRoot);

            // Folder name should be equal to 'folderName'
            var folderFilter = new SearchFilter.IsEqualTo(FolderSchema.DisplayName, folderName);

            // No need to look for more than one folder (can't have more than one folder with the exact same name)
            var findFoldersResults = rootFolder.FindFolders(folderFilter, new FolderView(1));

            if (!findFoldersResults.Any())
            {
                Logger.InfoFormat("Couldn't find folder {0}", folderName);
                return null;
            }

            Logger.DebugFormat("Found folder {0} ({1} matching folder items)", folderName, findFoldersResults.Count());

            var folder = findFoldersResults.First();
            return folder;
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(FolderNameResolver));
    }
}
