using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;


namespace LukeBot.Tests
{
    [TestClass]
    public class PasswordDataTests
    {
        const string TEST_PASSWORD = "testPassword1!2#34";
        const string TEST_PASSWORD_SHA512_STR = "17a36f3f68f6cf66d5999010c092c8f2e664000b01e2e1e2cbef8db49fa7a49f93588e96fa496ebd8157ee833223db1225aed8a02e50fe2a04d94e56ec308e98";

        static byte[] TEST_PASSWORD_SHA512; // acquired on test class startup


        [ClassInitialize]
        static public void PasswordData_TestClassStartup(TestContext context)
        {
            TEST_PASSWORD_SHA512 = Convert.FromHexString(TEST_PASSWORD_SHA512_STR);
        }

        [TestMethod]
        public void PasswordData_CreatePlain()
        {
            PasswordData data = PasswordData.Create(TEST_PASSWORD);

            // we should have a hash computed
            Assert.IsNotNull(data.Hash);

            // it should NOT directly match our SHA512 string
            Assert.IsFalse(TEST_PASSWORD_SHA512.SequenceEqual(data.Hash));
        }

        [TestMethod]
        public void PasswordData_CreateHash()
        {
            PasswordData data = PasswordData.Create(TEST_PASSWORD_SHA512);

            // we should have a hash computed
            Assert.IsNotNull(data.Hash);

            // it should NOT directly match our SHA512 string
            Assert.IsFalse(TEST_PASSWORD_SHA512.SequenceEqual(data.Hash));
        }

        [TestMethod]
        public void PasswordData_EqualsBytes()
        {
            PasswordData data = PasswordData.Create(TEST_PASSWORD);
            Assert.IsNotNull(data.Hash);

            Assert.IsTrue(data.Equals(TEST_PASSWORD_SHA512));
        }

        [TestMethod]
        public void PasswordData_EqualsString()
        {
            PasswordData data = PasswordData.Create(TEST_PASSWORD);
            Assert.IsNotNull(data.Hash);

            Assert.IsTrue(data.Equals(TEST_PASSWORD));
        }

        [TestMethod]
        public void PasswordData_CreateTwoSame()
        {
            PasswordData data = PasswordData.Create(TEST_PASSWORD);
            Assert.IsNotNull(data.Hash);

            PasswordData data2 = PasswordData.Create(TEST_PASSWORD);
            Assert.IsNotNull(data.Hash);

            // Two separate PasswordDatas created from the same plaintext password
            // should NOT produce the same final hash due to different random salt
            Assert.AreNotEqual(data, data2);
        }
    }
}