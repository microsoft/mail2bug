using System;
using System.Linq;
using System.Reflection;
using Mail2Bug.Helpers;
using Microsoft.Test.CommandLineParsing;

namespace DpapiHelperUtil
{
    class DpapiHelperUtilMain
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

            public override void Execute()
            {
                DPAPIHelper.WriteDataToFile(Data, Out);
            }
        }

        public class DpapiUtilReadCommand : Command
        {
            /// <summary>
            /// The name of the file to read the encrypted data from
            /// </summary>
            public string Filename { get; set; }

            public override void Execute()
            {
                string readDataFromFile = DPAPIHelper.ReadDataFromFile(Filename);
                Console.WriteLine("Data:'{0}'", readDataFromFile);
            }
        }


        static void Main(string[] args)
        {
            if(args.Length < 1)
            {
                PrintUsage();
                return;
            }

            Command c = null;
            if (args[0].Equals("read", StringComparison.InvariantCultureIgnoreCase))
            {
                c = new DpapiUtilReadCommand();
            }

            if (args[0].Equals("write", StringComparison.InvariantCultureIgnoreCase))
            {
                c = new DpapiUtilWriteCommand();
            }

            if (c == null)
            {
                PrintUsage();
                return;
            }

            c.ParseArguments(args.Skip(1));
            c.Execute();
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("DpapiHelperUtil read /Filename=<filename>");
            Console.WriteLine("DpapiHelperUtil write /Data=<data> /Out=<filename>");
        }
    }
}
