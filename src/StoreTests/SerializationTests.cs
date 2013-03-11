using System;
using Daytona.Store;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using TestHelpers;
using Daytona;

namespace StoreTests
{
    [TestClass]
    public class SerializationTests
    {
        [TestMethod]
        public void SerializerTest()
        {
            var serializer = new Daytona.Store.Serializer(Encoding.Unicode);
            var cust = new Customer
            {
                Firstname = "Jie",
                Lastname = "wilson"
            };
            var pl = new DBPayload<Customer>();
            pl.Payload = cust;
            var input = serializer.GetSerializedPayload<Customer>(pl);

            var output = serializer.Deserializer<DBPayload<Customer>>(input);
            Assert.AreEqual (pl.Payload.Firstname, output.Payload.Firstname);
            Assert.AreEqual(pl.Payload.Lastname, output.Payload.Lastname);
        }

        [TestMethod]
        public void SerializerTest2()
        {
            var serializer = new Daytona.Store.Serializer(Encoding.Unicode);
            var cust = new Customer
            {
                Firstname = "Jie",
                Lastname = "wilson"
            };
            var pl = new DBPayload<Customer>();
            pl.Payload = cust;
            var input = serializer.GetSerializedPayload<Customer>(pl);

            var output = serializer.Deserializer<DBPayload<Customer>>(input);
            Assert.AreEqual(pl.Payload.Firstname, output.Payload.Firstname);
            Assert.AreEqual(pl.Payload.Lastname, output.Payload.Lastname);
        }
    }
}
