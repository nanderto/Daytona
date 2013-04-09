using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Daytona.Store;
using System.Text;
using TestHelpers;

namespace StoreTests
{
    [TestClass]
    public class WriterTests
    {
        [TestMethod]
        public void SaveTest()
        {
            var esentInstance = EsentInstanceService.Service.EsentInstance;
            var writer = new Writer(esentInstance);
            var result = writer.Save<Customer>(Encoding.Unicode.GetBytes("this is my message"));
            Assert.AreNotEqual(result, 23);
        }
    }
}
