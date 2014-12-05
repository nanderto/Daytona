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

    using NetMQ;
    using NetMQ.Devices;

    using TestHelpers;

    [TestClass]
    public class ActorTests
    {
        [TestMethod]
        public void CallMethod_usingDefaultSerializer()
        {
            using (var context = NetMQContext.Create())
            {
                using (var actor = new Actor<Customer>(context))
                {
                    var customer = actor.CreateInstance<ICustomer>(typeof(Customer));
                    Assert.IsInstanceOfType(customer, typeof(ICustomer));
                    customer.UpdateName("XXX"); //called without exception
                }
            }
        }

        [TestMethod]   
        public void CallMethod_UsingBinarySerializer()
        {
            using (var context = NetMQContext.Create())
            {
                using (var actor = new Actor<Order>(context, new BinarySerializer()))
                {
                    var customer = actor.CreateInstance<ICustomer>(typeof(Customer));
                    Assert.IsInstanceOfType(customer, typeof(ICustomer));
                    customer.UpdateName("XXX"); //called without exception
                }
            }
        }

        [TestMethod]
        public void CallMethod_Multiple_ObjectsBinarySerializer()
        {
            using (var context = NetMQContext.Create())
            {
                var exchange = new Exchange(context, Pipe.SubscribeAddress, Pipe.PublishAddress, DeviceMode.Threaded);
                exchange.Start();

                using (var actor = new Actor<Customer>(context, new BinarySerializer()))
                {
                    var customer = actor.CreateInstance<ICustomer>(typeof(Customer), 33);
                    Assert.IsInstanceOfType(customer, typeof(ICustomer));
                    customer.UpdateName("XXX"); //called without exception

                    var order = actor.CreateInstance<IOrder>(typeof(Order));
                    Assert.IsInstanceOfType(order, typeof(IOrder));
                    order.UpdateDescription("XXX"); //called without exception

                    var order2 = actor.CreateInstance<IOrder>(typeof(Order), Guid.NewGuid());
                    Assert.IsInstanceOfType(order2, typeof(IOrder));
                    order2.UpdateDescription("ZZZ"); //called without exception
                }

                exchange.Stop();
            }
        }

        //[TestMethod()]
        //public void RegisterActorTest()
        //{
        //    using (var context = NetMQContext.Create())
        //    {
        //        var actor = new Actor<Customer>(context);
        //        var actorCustomer = actor.RegisterActor<Customer>(new Customer());
        //        Assert.IsInstanceOfType(actorCustomer, typeof(Actor<Customer>));
        //    }
        //}

        [TestMethod()]
        public async Task ReceiveMessageTest()
        {
            //using (var context = NetMQContext.Create())
            //{
            //    using (var actor = new Actor<Customer>(context, new BinarySerializer()))
            //    {
            //       // await Task.Run(async () => actor.Start());
            //        var customer = actor.CreateInstance<ICustomer>();
            //        Assert.IsInstanceOfType(customer, typeof(ICustomer));
            //        var x = typeof(Customer);
            //        var methodInfo = x.GetMethod("UpdateName");
            //        object[] parmeters = new object[1];
            //        parmeters[0] = "XXX"; 
            //        actor.SendMessage(parmeters, methodInfo);
            //        var stopSignal = false;
            //        //actor.ReceiveMessage(actor.subscriber, out stopSignal, new BinarySerializer());

            //        customer.UpdateName("XXX"); //called without exception
            //    }
            //}
        }

        [TestMethod]
        public void TestDeserialization()
        {
            var x = typeof(Customer);
            var methodInfo = x.GetMethod("UpdateName");
            object[] parmeters = new object[1];
            parmeters[0] = "XXX";

            //var customer = new Actor<Customer>(new BinarySerializer());

            var zmqMessage = Actor<Customer>.PackZmqMessage(parmeters, methodInfo, new BinarySerializer(), x.FullName);


            int frameCount = 0;
            var stopSignal = false;
            var zmqOut = new NetMQMessage();
            bool hasMore = true;
            //var address = string.Empty;
            byte[] messageAsBytes = null;
            
            int numberOfParameters = 0;
            MethodInfo methodinfo = null;
            List<object> methodParameters = new List<object>();
            var serializer = new BinarySerializer();
            var typeParameter = true;
            Type type = null;
            MethodInfo returnedMethodInfo = null;
            string address, returnedAddress, messageType, returnedMessageType = string.Empty;

            foreach (var frame in zmqMessage)
            {
                stopSignal = Actor<Customer>.UnPackNetMQFrame(frameCount, serializer, frame, out address, ref methodinfo, methodParameters, ref typeParameter, ref type, out messageType);
                if (frameCount == 0)
                {
                    returnedAddress = address;
                }

                if (frameCount == 1)
                {
                    returnedMessageType = messageType;
                }

                if (frameCount == 2)
                {
                    returnedMethodInfo = methodinfo;
                }

                frameCount++;
                zmqOut.Append(new NetMQFrame(frame.Buffer));
               // hasMore = subscriber.ReceiveMore;
            }
            
            var target = (Customer)Activator.CreateInstance(typeof(Customer));
            var result = (Customer)returnedMethodInfo.Invoke(target, methodParameters.ToArray());
            Assert.AreEqual("XXX", target.Lastname);
        }

        private static MethodInfo UnPackNetMQFrame(
            int FrameCount,
            ref bool stopSignal,
            BinarySerializer serializer,
            NetMQFrame frame,
            MethodInfo methodinfo,
            List<object> methodParameters,
            ref bool typeParameter,
            ref Type type)
        {
            string address;
            byte[] messageAsBytes;
            int numberOfParameters;
            if (FrameCount == 0)
            {
                address = serializer.GetString(frame.Buffer);
            }

            if (FrameCount == 1)
            {
                messageAsBytes = frame.Buffer;
                string stopMessage = serializer.GetString(messageAsBytes);
                if (stopMessage.ToLower() == "stop")
                {
                    stopSignal = true;
                }
            }

            if (FrameCount == 2)
            {
                methodinfo = (MethodInfo)serializer.Deserializer(frame.Buffer, typeof(MethodInfo));
            }

            if (FrameCount == 3)
            {
                numberOfParameters = int.Parse(serializer.GetString(frame.Buffer).Replace("ParameterCount:", string.Empty));
            }

            if (FrameCount > 3)
            {
                if (typeParameter)
                {
                    type = (Type)serializer.Deserializer(frame.Buffer, typeof(Type));
                    typeParameter = false;
                }
                else
                {
                    var parameter = serializer.Deserializer(frame.Buffer, type);
                    methodParameters.Add(parameter);
                    typeParameter = true;
                }
            }
            return methodinfo;
        }
    }
}
