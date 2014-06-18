
namespace Daytona.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Daytona;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TestHelpers;

    using ZeroMQ;

    [TestClass]
    public class ActorTests
    {
        [TestMethod]
        public void CreateInstanceTest()
        {
            using (var context = ZmqContext.Create())
            {
                var actor = new Actor<Customer>(context);
                var customer = actor.CreateInstance<Customer>();
                Assert.IsInstanceOfType(customer, typeof(Customer));
            }
            Assert.Fail();
        }
    }
}
