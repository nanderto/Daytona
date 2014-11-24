using Daytona;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        }

        [TestMethod]
        public void CallMethod()
        {
            //using (var context = ZmqContext.Create())
            //{
            //    var actor = new Actor<Customer>(context);
            //    var customer = actor.CreateInstance<ICustomer>();
            //    Assert.IsInstanceOfType(customer, typeof(ICustomer));
            //    customer.UpdateName("XXX"); //called without exception
            //}
        }

        [TestMethod]   
        public void CallMethod_usingBinarySerializer()
        {
            using (var context = ZmqContext.Create())
            {
                using (var actor = new Actor<Customer>(context, new BinarySerializer()))
                {
                    var customer = actor.CreateInstance<ICustomer>();
                    Assert.IsInstanceOfType(customer, typeof(ICustomer));
                    customer.UpdateName("XXX"); //called without exception
                }
            }
        }

        //[TestMethod()]
        //public void RegisterActorTest()
        //{
        //    using (var context = ZmqContext.Create())
        //    {
        //        var actor = new Actor<Customer>(context);
        //        var actorCustomer = actor.RegisterActor<Customer>(new Customer());
        //        Assert.IsInstanceOfType(actorCustomer, typeof(Actor<Customer>));
        //    }
        //}

        [TestMethod()]
        public void ReceiveMessageTest()
        {
            using (var context = ZmqContext.Create())
            {
                using (var actor = new Actor<Customer>(context, new BinarySerializer()))
                {
                    actor.Start();
                    var customer = actor.CreateInstance<ICustomer>();
                    Assert.IsInstanceOfType(customer, typeof(ICustomer));
                    var x = customer.GetType();
                    var methodInfo = x.GetMethod("UpdateName");
                    object[] parmeters = new object[1];
                    parmeters[0] = "XXX"; 
                    actor.SendMessage(parmeters, methodInfo);
                    var stopSignal = false;
                    actor.ReceiveMessage(actor.subscriber, out stopSignal, new BinarySerializer());

                    customer.UpdateName("XXX"); //called without exception
                }
            }
        }
    }
}
