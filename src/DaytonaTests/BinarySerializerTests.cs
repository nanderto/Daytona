namespace Daytona.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    using Daytona;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using TestHelpers;

    [TestClass]
    public class BinarySerializerTests
    {
        [TestMethod]
        public void GetBuffer_UseGeneric()
        {
            var binarySerializer = new BinarySerializer();
            var buffer = binarySerializer.GetBuffer(new TestHelpers.Customer());
            var customer = binarySerializer.Deserializer<Customer>(buffer);
            Assert.IsInstanceOfType(customer, typeof(Customer));
        }

        [TestMethod]
        public void GetBufferTest()
        {
            var binarySerializer = new BinarySerializer();
            var buffer = binarySerializer.GetBuffer(new TestHelpers.Customer());
            var customer = binarySerializer.Deserializer(buffer, typeof(Customer));
            Assert.IsInstanceOfType(customer, typeof(Customer));
        }

        [TestMethod]
        public void SerializeMethodInfo()
        {
            var customer = new Customer();
            var method = typeof(Customer).GetMethod("UpdateName");
            var binarySerializer = new BinarySerializer();
            var buffer = binarySerializer.GetBuffer(method);
            var methodInfo = (MethodInfo)binarySerializer.Deserializer(buffer, typeof(MethodInfo));
            Assert.IsInstanceOfType(methodInfo, typeof(MethodInfo));
            Assert.AreEqual("UpdateName", methodInfo.Name);
        }

        [TestMethod]
        public void Call_SerializeandDeserialisedMethodInfo_Method_Successful()
        {
            var customer = new Customer();
            var method = typeof(Customer).GetMethod("UpdateName");
            var binarySerializer = new BinarySerializer();
            var buffer = binarySerializer.GetBuffer(method);
            var methodInfo = (MethodInfo)binarySerializer.Deserializer(buffer, typeof(MethodInfo));
            Assert.IsInstanceOfType(methodInfo, typeof(MethodInfo));
            Assert.AreEqual("UpdateName", methodInfo.Name);
            object[] parameters = new object[1];
            parameters[0] = "XXXX";
            var result = methodInfo.Invoke(customer, parameters);
            Assert.AreEqual("XXXX", customer.Lastname);
        }

    }
}
