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
        public async Task ReceiveMessageTest()
        {
            using (var context = ZmqContext.Create())
            {
                using (var actor = new Actor<Customer>(context, new BinarySerializer()))
                {
                   // await Task.Run(async () => actor.Start());
                    var customer = actor.CreateInstance<ICustomer>();
                    Assert.IsInstanceOfType(customer, typeof(ICustomer));
                    var x = typeof(Customer);
                    var methodInfo = x.GetMethod("UpdateName");
                    object[] parmeters = new object[1];
                    parmeters[0] = "XXX"; 
                    actor.SendMessage(parmeters, methodInfo);
                    var stopSignal = false;
                    //actor.ReceiveMessage(actor.subscriber, out stopSignal, new BinarySerializer());

                    customer.UpdateName("XXX"); //called without exception
                }
            }
        }

        [TestMethod]
        public void TestDeserialization()
        {
            var x = typeof(Customer);
            var methodInfo = x.GetMethod("UpdateName");
            object[] parmeters = new object[1];
            parmeters[0] = "XXX";
            
            var zmqMessage = new ZmqMessage();
            var address = x.FullName;
            zmqMessage.Append(new Frame(new BinarySerializer().GetBuffer(address)));
            zmqMessage.Append(new Frame(new BinarySerializer().GetBuffer("Process")));

            // var binarySerializer = new BinarySerializer();
            // var buffer = binarySerializer.GetBuffer(methodInfo);
            var serializedMethodInfo = new BinarySerializer().GetBuffer(methodInfo);
            zmqMessage.Append(new Frame(serializedMethodInfo));
            zmqMessage.Append(
                new Frame(new BinarySerializer().GetBuffer(string.Format("ParameterCount:{0}", parmeters.Length))));
            zmqMessage.Append(new BinarySerializer().GetBuffer("XXX".GetType()));
            zmqMessage.Append(new BinarySerializer().GetBuffer("XXX"));

            
            //foreach (var parameter in parameters)
            //{
            //    zmqMessage.Append(new BinarySerializer().GetBuffer(parameter.GetType()));
            //    zmqMessage.Append(new BinarySerializer().GetBuffer(parameter));
            //}
            var stopSignal = false;
            var zmqOut = new ZmqMessage();
            bool hasMore = true;
            //var address = string.Empty;
            byte[] messageAsBytes = null;
            int i = 0;
            int numberOfParameters = 0;
            MethodInfo methodinfo = null;
            List<object> methodParameters = new List<object>();
            var serializer = new BinarySerializer();
            var typeParameter = true;
            Type type = null;

            foreach (var parmeter in zmqMessage)
            {
                if (i == 0)
                {
                    address = serializer.GetString(parmeter.Buffer);
                }

                if (i == 1)
                {
                    messageAsBytes = parmeter.Buffer;
                    string stopMessage = serializer.GetString(messageAsBytes);
                    if (stopMessage.ToLower() == "stop")
                    {
                        stopSignal = true;
                    }
                }

                if (i == 2)
                {
                    methodinfo = (MethodInfo)serializer.Deserializer(parmeter.Buffer, typeof(MethodInfo));
                }

                if (i == 3)
                {
                    numberOfParameters = int.Parse(serializer.GetString(parmeter.Buffer).Replace("ParameterCount:", string.Empty));
                }

                if (i > 3)
                {
                    if (typeParameter)
                    {
                        type = (Type)serializer.Deserializer(parmeter.Buffer, typeof(Type));
                        typeParameter = false;
                    }
                    else
                    {
                        var parameter = serializer.Deserializer(parmeter.Buffer, type);
                        methodParameters.Add(parameter);
                        typeParameter = true;
                    }                 
                }

                i++;
                zmqOut.Append(new Frame(parmeter.Buffer));
            }

            var result = methodinfo.Invoke((Customer)Activator.CreateInstance(typeof(Customer)), methodParameters.ToArray());
            //return result;

        }
    }
}
