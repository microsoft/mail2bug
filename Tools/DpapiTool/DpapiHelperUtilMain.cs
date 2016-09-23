using System;
using System.Linq;
using System.Reflection;
using Mail2Bug.Helpers;
using Microsoft.Test.CommandLineParsing;
using System.Security.Cryptography;

namespace DpapiHelperUtil
{
    public class DpapiHelperUtilMain
    {
        public class DpapiUtilWriteCommand : Command
        {
            /// <summary>
            /// The data to write
            /// </summary>
            public string Data { get; set; }

            /// <summary>
            /// The name of the file to write the encrypted data to
            /// </summary>
            public string Out { get; set; }

            /// <summary>
            /// The scope of the DataProtection that will be used
            /// </summary>
            public DataProtectionScope Scope { get; private set; }

            public override void Execute()
            {
                DPAPIHelper.WriteDataToFile(Data, Out, Scope);
            }

            public DpapiUtilWriteCommand(DataProtectionScope scope)
            {
                this.Scope = scope;
            }
        }

        public class DpapiUtilReadCommand : Command
        {
            /// <summary>
            /// The name of the file to read the encrypted data from
            /// </summary>
            public string Filename { get; set; }

            /// The scope of the DataProtection that was
            public DataProtectionScope Scope { get; private set; }

            public override void Execute()
            {
                string readDataFromFile = DPAPIHelper.ReadDataFromFile(Filename, Scope);
                Console.WriteLine("Data:'{0}'", readDataFromFile);
            }
            public DpapiUtilReadCommand(DataProtectionScope scope)
            {
                this.Scope = scope;
            }
        }


        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                PrintUsage();
                return;
            }

            // parse user intent
            var command = ArgumentParserHelper.ParseArguments(args);

            // if null, user intent is unclear or arguments were missed
            if (command == null)
            {
                PrintUsage();
                return;
            }

            // execute command
            command.Execute();
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("DpapiHelperUtil read [user|machine] /Filename=<filename>");
            Console.WriteLine("DpapiHelperUtil write [user|machine] /Data=<data> /Out=<filename>");
            Console.WriteLine("");
            Console.WriteLine("Note: user|machine is the password encryption scope and is optional.");
            Console.WriteLine("\tuser - Default. Will encrypt with current executing user scope");
            Console.WriteLine("\tmachine - Will encrypt with current machine scope");
        }
    }
}
