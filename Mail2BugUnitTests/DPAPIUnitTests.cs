using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mail2Bug.Helpers;
using System.Security.Cryptography;
using System.Security.Principal;

namespace Mail2BugUnitTests
{
    [TestClass]
    public class DPAPIUnitTests
    {
        #region Encode/Decode tests
        [TestMethod]
        public void TestSimpleEncryptionSucceedsDefaultScope()
        {
            var sourceString = "Hello world!@#";

            var encryptedData = DPAPIHelper.Encrypt(sourceString, null, DataProtectionScope.CurrentUser);
            var decryptedString = DPAPIHelper.Decrypt(encryptedData, null, DataProtectionScope.CurrentUser);
            Assert.AreEqual<string>(sourceString, decryptedString);
        }

        [TestMethod]
        public void TestSimpleEncryptionSucceedsMachineScope()
        {
            var sourceString = "Hello world!@#";

            var encryptedData = DPAPIHelper.Encrypt(sourceString, null, DataProtectionScope.LocalMachine);
            var decryptedString = DPAPIHelper.Decrypt(encryptedData, null, DataProtectionScope.LocalMachine);
            Assert.AreEqual<string>(sourceString, decryptedString);
        }

        #endregion

        #region "read" tests
        [TestMethod]
        public void TestCommandLineReadSimple_1()
        {
            var fn = "foobar.txt";
            var cl = string.Format("read /Filename={0}", fn);
            var command = DpapiHelperUtil.ArgumentParserHelper.ParseArguments(cl.Split(' '));
            Assert.IsNotNull(command);
            Assert.IsTrue(command is DpapiHelperUtil.DpapiHelperUtilMain.DpapiUtilReadCommand);
            var typedCommand = command as DpapiHelperUtil.DpapiHelperUtilMain.DpapiUtilReadCommand;
            Assert.AreEqual(typedCommand.Filename, fn);
            Assert.AreEqual(typedCommand.Scope, DataProtectionScope.CurrentUser);
        }

        [TestMethod]
        public void TestCommandLineReadSimple_2()
        {
            var fn = "foobar.txt";
            var cl = string.Format("read user /Filename={0}", fn);
            var command = DpapiHelperUtil.ArgumentParserHelper.ParseArguments(cl.Split(' '));
            Assert.IsNotNull(command);
            Assert.IsTrue(command is DpapiHelperUtil.DpapiHelperUtilMain.DpapiUtilReadCommand);
            var typedCommand = command as DpapiHelperUtil.DpapiHelperUtilMain.DpapiUtilReadCommand;
            Assert.AreEqual(typedCommand.Filename, fn);
            Assert.AreEqual(typedCommand.Scope, DataProtectionScope.CurrentUser);
        }

        [TestMethod]
        public void TestCommandLineReadSimple_3()
        {
            var fn = "foobar.txt";
            var cl = string.Format("read machine /Filename={0}", fn);
            var command = DpapiHelperUtil.ArgumentParserHelper.ParseArguments(cl.Split(' '));
            Assert.IsNotNull(command);
            Assert.IsTrue(command is DpapiHelperUtil.DpapiHelperUtilMain.DpapiUtilReadCommand);
            var typedCommand = command as DpapiHelperUtil.DpapiHelperUtilMain.DpapiUtilReadCommand;
            Assert.AreEqual(typedCommand.Filename, fn);
            Assert.AreEqual(typedCommand.Scope, DataProtectionScope.LocalMachine);
        }

        #endregion

        #region "write" tests
        [TestMethod]
        public void TestCommandLineWriteSimple_1()
        {
            var data = "P@ssword";
            var fn = "foobar.txt";
            var cl = string.Format("write /Data={0} /Out={1}", data, fn);
            var command = DpapiHelperUtil.ArgumentParserHelper.ParseArguments(cl.Split(' '));
            Assert.IsNotNull(command);
            Assert.IsTrue(command is DpapiHelperUtil.DpapiHelperUtilMain.DpapiUtilWriteCommand);
            var typedCommand = command as DpapiHelperUtil.DpapiHelperUtilMain.DpapiUtilWriteCommand;
            Assert.AreEqual(typedCommand.Out, fn);
            Assert.AreEqual(typedCommand.Data, data);
            Assert.AreEqual(typedCommand.Scope, DataProtectionScope.CurrentUser);
        }

        [TestMethod]
        public void TestCommandLineWriteSimple_2()
        {
            var data = "P@ssword";
            var fn = "foobar.txt";
            var cl = string.Format("write user /Data={0} /Out={1}", data, fn);
            var command = DpapiHelperUtil.ArgumentParserHelper.ParseArguments(cl.Split(' '));
            Assert.IsNotNull(command);
            Assert.IsTrue(command is DpapiHelperUtil.DpapiHelperUtilMain.DpapiUtilWriteCommand);
            var typedCommand = command as DpapiHelperUtil.DpapiHelperUtilMain.DpapiUtilWriteCommand;
            Assert.AreEqual(typedCommand.Out, fn);
            Assert.AreEqual(typedCommand.Data, data);
            Assert.AreEqual(typedCommand.Scope, DataProtectionScope.CurrentUser);
        }

        [TestMethod]
        public void TestCommandLineWriteSimple_3()
        {
            var data = "P@ssword";
            var fn = "foobar.txt";
            var cl = string.Format("write machine /Data={0} /Out={1}", data, fn);
            var command = DpapiHelperUtil.ArgumentParserHelper.ParseArguments(cl.Split(' '));
            Assert.IsNotNull(command);
            Assert.IsTrue(command is DpapiHelperUtil.DpapiHelperUtilMain.DpapiUtilWriteCommand);
            var typedCommand = command as DpapiHelperUtil.DpapiHelperUtilMain.DpapiUtilWriteCommand;
            Assert.AreEqual(typedCommand.Out, fn);
            Assert.AreEqual(typedCommand.Data, data);
            Assert.AreEqual(typedCommand.Scope, DataProtectionScope.LocalMachine);
        }

        #endregion
    }
}
