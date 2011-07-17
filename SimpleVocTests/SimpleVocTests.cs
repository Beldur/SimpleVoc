using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleVoc;

namespace SimpleVocTests
{
    [TestClass]
    public class SimpleVocTests
    {
        private SimpleVocConnection _simpleVoc;

        [TestInitialize]
        public void TestInitialization()
        {
            _simpleVoc = new SimpleVocConnection("192.168.178.20", 8008);
            _simpleVoc.Flush();
        }

        [TestMethod]
        public void Test_Server_Version()
        {
            var version = _simpleVoc.Version;

            Assert.IsTrue(!string.IsNullOrWhiteSpace(version), "Version should have a value.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Test_Set_With_Empty_Key()
        {
            var value = new SimpleVocValue();

            _simpleVoc.Set(value);
        }

        [TestMethod]
        public void Test_Set()
        {
            var value = new SimpleVocValue()
            {
                Data = "Test Data",
                Key = "TestKey"
            };

            var result = _simpleVoc.Set(value);

            Assert.IsTrue(result, "Result for setting a value should be true.");
        }

        [TestMethod]
        [ExpectedException(typeof(SimpleVocException))]
        public void Test_Set_Same_Key_Twice()
        {
            var value = new SimpleVocValue()
            {
                Data = "Test Data",
                Key = "TestKey"
            };

            var result  = _simpleVoc.Set(value);
            var result2 = _simpleVoc.Set(value);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Test_Get_With_Empty_Key()
        {
            var result = _simpleVoc.Get("");
        }

        [TestMethod]
        [ExpectedException(typeof(SimpleVocException))]
        public void Test_Get_With_Non_Existend_Key()
        {
            var result = _simpleVoc.Get("NotExistend");
        }

        [TestMethod]
        public void Test_Set_And_Get()
        {
            var expires = DateTime.Now.AddDays(1);

            var testData = new SimpleVocValue()
            {
                Key = "TestGetKey",
                Data = "♥ Some random Data ♥",
                Flags = 123,
                Expires = expires
            };

            _simpleVoc.Set(testData);

            var result = _simpleVoc.Get(testData.Key);

            Assert.AreEqual(testData.Key, result.Key);
            Assert.AreEqual(testData.Data, result.Data);
            Assert.AreEqual(testData.Flags, result.Flags);
            Assert.AreEqual(testData.Expires.ToString(), result.Expires.ToString());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Test_GetKeys_With_Empty_Prefix()
        {
            _simpleVoc.GetKeys("");
        }

        [TestMethod]
        public void Test_GetKeys()
        {
            _simpleVoc.Set(new SimpleVocValue() { Key = "TestKey" });
            _simpleVoc.Set(new SimpleVocValue() { Key = "TestCey" });

            var result = _simpleVoc.GetKeys("T");
            Assert.AreEqual(result.Length, 2);

            result = _simpleVoc.GetKeys("TestK");
            Assert.AreEqual(result.Length, 1);

            result = _simpleVoc.GetKeys("A");
            Assert.AreEqual(result.Length, 0);
        }
    }
}