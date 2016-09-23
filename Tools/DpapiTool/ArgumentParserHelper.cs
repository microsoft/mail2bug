using Microsoft.Test.CommandLineParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static DpapiHelperUtil.DpapiHelperUtilMain;

namespace DpapiHelperUtil
{

    public static class ArgumentParserHelper
    {
        /// <summary>
        /// Helper to determine and set arguments
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static Command ParseArguments(string[] args)
        {
            Command resultCommand = null;

            // skip the "read/write" arg
            var argsToSkip = 1;
            DataProtectionScope scope = DataProtectionScope.CurrentUser;

            // check if we have additional arguments
            if (args.Length > 1)
            {
                if (args[1].Equals("machine", StringComparison.InvariantCultureIgnoreCase))
                {
                    scope = DataProtectionScope.LocalMachine;

                    // also skip the "machine" word
                    argsToSkip++;
                }
                if (args[1].Equals("user", StringComparison.InvariantCultureIgnoreCase))
                {
                    // skip "user" word
                    argsToSkip++;
                }
            }

            if (args[0].Equals("read", StringComparison.InvariantCultureIgnoreCase))
            {
                resultCommand = new DpapiUtilReadCommand(scope);
            }

            if (args[0].Equals("write", StringComparison.InvariantCultureIgnoreCase))
            {
                resultCommand = new DpapiUtilWriteCommand(scope);
            }

            // if we have a "real" known command, then try to parse rest of the arguments. Otherwise it doesn't matter
            if (resultCommand != null)
                resultCommand.ParseArguments(args.Skip(argsToSkip));

            return resultCommand;
        }
    }
}
