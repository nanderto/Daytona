using Daytona;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Daytona.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Daytona;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NetMQ;
    using NetMQ.Devices;
    using NetMQ.zmq;

    using TestHelpers;
    
    [TestClass]
    public class ActorTests
    {
        public static AutoResetEvent waitHandle = new AutoResetEvent(false);

       [TestMethod]   
       public void CallMethod_Using_NProxyWrapper_ReadMessageWithRawActor()
       {
            waitHandle.Reset();
            using (var DaytonaContext = Context.Create(new ConsoleMonitor()))
            {                   
                var queueDevice = new QueueDevice(
                DaytonaContext.NetMqContext,
                Exchange.PubSubControlBackAddressServer,
                Exchange.PubSubControlFrontAddressServer,
                DeviceMode.Threaded);
                queueDevice.Start();

                Thread.Sleep(200);

                var task = Task.Run(() =>
                {
                    return RunSubscriber(DaytonaContext.NetMqContext);
                });

                using (var actor = new Actor(DaytonaContext.NetMqContext, new BinarySerializer()))
                {
                    using (var syncService = DaytonaContext.NetMqContext.CreateResponseSocket())
                    {
                        syncService.Connect(Exchange.PubSubControlFrontAddressClient);
                        for (int i = 0; i < 1; i++)
                        {
                            syncService.Receive();
                            syncService.Send(string.Empty);
                        }

                        var order = actor.CreateInstance<IOrder>(typeof(Order));
                        Assert.IsInstanceOfType(order, typeof(IOrder));
                        order.UpdateDescription("XXX"); //called without exception    
                        waitHandle.WaitOne();

                        var netMqMessage = new NetMQMessage();
                        netMqMessage.Append(new NetMQFrame(string.Empty));
                        netMqMessage.Append(new NetMQFrame("shutdownallactors"));
                        actor.OutputChannel.SendMessage(netMqMessage);

                        //actor.SendKillSignal(actor.Serializer, actor.OutputChannel, string.Empty);
                    }
                }

                Thread.Sleep(200);

                queueDevice.Stop(true);
            }
        }

       private Task RunSubscriber(NetMQContext context)
       {
            using (NetMQSocket syncClient = context.CreateRequestSocket())
            {
                syncClient.Connect(Exchange.PubSubControlBackAddressClient);
                syncClient.Send(string.Empty);
                syncClient.Receive();
                
                var actions = new Dictionary<string, Delegate>();
                actions.Add("MethodInfo", TestActors);
                actions.Add("ShutDownAllActors", ShutDownAllActors);

                using (var actor = new Actor<Order>(context, new BinarySerializer()))
                {
                    actor.RegisterActor(
                        "Display",
                        string.Empty,
                        "outRoute",
                        null,
                        new BinarySerializer(),
                        new DefaultSerializer(Exchange.ControlChannelEncoding),
                        actions);

                    actor.StartAllActors();
                }               
            }    

            return null;
        }

       public static Action<string, Actor> ShutDownAllActors = (instruction, actor) =>
       {
           object returnedObject = null;
           List<RunningActors> runningActors = null;

           if (actor.PropertyBag.TryGetValue("RunningActors", out returnedObject))
           {
               runningActors = (List<RunningActors>)returnedObject;
               foreach (var actr in runningActors)
               {
                   actor.SendKillSignal(actor.Serializer, actor.OutputChannel, actr.Address);
               }
           }
       };

       public static Action<string, string, MethodInfo, List<object>, Actor> TestActors =
            (address, returnAddress, methodinfo, parameters, actr) =>
                {
                    var firstParameter = string.Empty;
                    try
                    {
                        firstParameter = parameters[0].ToString();
                    }
                    catch (Exception)
                    {
                    }

                    Console.WriteLine("Address: {0}, {1}", address, firstParameter);
                    actr.WriteLineToMonitor(string.Format("Address: {0}, {1}", address, firstParameter));
                    waitHandle.Set();
                };

       // [TestMethod]
       /// <summary>
       /// /this method does not test what it is supposed to test
       /// </summary>
        public void CallKillMe()
        {
            using (var context = NetMQContext.Create())
            {
                using (var exchange = new Exchange(context))
                {
                    exchange.Start();
                    using (var customer = new Actor<Customer>(context, new BinarySerializer()))
                    {
                        Thread.Sleep(300);
                        Task.Run(() => customer.Start());
                    }

                    exchange.Stop(true);
                }
            }
        }

        [TestMethod]
        public void CallMethod_Multiple_ObjectsBinarySerializer()
        {
            using (var DaytonaContext = Context.Create(new ConsoleMonitor()))
            {
                //var exchange = new XForwarder(DaytonaContext.NetMqContext, Exchange.SubscribeAddress, Exchange.PublishAddress, DeviceMode.Threaded);
                //exchange.Start();

                using (var actor = new Actor<Customer>(DaytonaContext.NetMqContext, new BinarySerializer()))
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

               // exchange.Stop();
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

            var zmqMessage = Actor.PackZmqMessage(parmeters, methodInfo, new BinarySerializer(), x.FullName);


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
            //List<object> parameters = new List<object>();

            returnedAddress = getString(zmqMessage, serializer);
            returnedMessageType = getString(zmqMessage, serializer);
            if (returnedMessageType == "MethodInfo")
            {
                returnedMethodInfo = getMethodInfo(zmqMessage, serializer);
                while (AddParameter(zmqMessage, serializer, methodParameters))
                {
                }
            }
            
            var target = (Customer)Activator.CreateInstance(typeof(Customer));
            var result = (Customer)returnedMethodInfo.Invoke(target, methodParameters.ToArray());
            Assert.AreEqual("XXX", target.Lastname);
        }

        //public static IEnumerable<T2> MySelect<T1, T2>(this IEnumerable<T1> data, Func<T1, T2> f)
        //{
        //    List<T2> retVal = new List<T2>();
        //    foreach (T1 x in data) retVal.Add(f(x));
        //    return retVal;
        //}

        private Func<NetMQMessage, BinarySerializer, MethodInfo> getMethodInfo = (socket, serializer) =>
        {
            //var hasMore = false;
            //var buffer = socket.Receive(out hasMore);
            var frame = socket.First();
            var buffer = frame.Buffer;
            socket.RemoveFrame(frame);
            return (MethodInfo)serializer.Deserializer(buffer, typeof(MethodInfo));
        };

        private Func<NetMQMessage, BinarySerializer, string> getString = (socket, serializer) =>
        {
           // var hasMore = false;
           // var buffer = socket.Receive(out hasMore);
            var frame = socket.First();
            var buffer = frame.Buffer;
            socket.RemoveFrame(frame);
            return serializer.GetString(buffer);
        };

        private Func<NetMQMessage, BinarySerializer, List<object>,  bool> AddParameter = (socket, serializer, parameters) =>
            {
                Type returnedType = getType(socket, serializer);
                object parameter = null;
                var result = getParameter(socket, serializer, returnedType);
                parameters.Add(result.Item1);
                return result.Item2;
            };

        private static Func<NetMQMessage, BinarySerializer, Type> getType = (socket, serializer) =>
        {
           // var hasMore = false;
           // var buffer = socket.Receive(out hasMore);
            var frame = socket.First();
            var buffer = frame.Buffer;
            socket.RemoveFrame(frame);
            return (Type)serializer.Deserializer(buffer, typeof(Type));
        };

         private static Func<NetMQMessage, BinarySerializer, Type, Tuple<object, bool>> getParameter = (socket, serializer, type) =>
        {
            var hasMore = true;
           // var buffer = socket.Receive(out hasMore);
            
            var frame = socket.First();
            var buffer = frame.Buffer;
            socket.RemoveFrame(frame);
            if (socket.Count() == 0)
            {
                hasMore = false;
            }

            var parameter = serializer.Deserializer(buffer, type);
            return new Tuple<object, bool>(parameter, hasMore);
        };

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

        [TestMethod()]
        public void PersistSelfTest()
        {

            using (var context = NetMQContext.Create())
            {
                var consoleMonitor = new ConsoleMonitor();
                consoleMonitor.Start(context);
                using (var exchange = new Exchange(context))
                {
                    exchange.Start();

                    using (var actor = new Actor<Customer>(context))
                    {
                        var customer = new Customer(1);
                        customer.Firstname = "John";
                        customer.Lastname = "off yer Rocker mate";

                        actor.PersistSelf(typeof(Customer), customer, new DefaultSerializer(Exchange.ControlChannelEncoding));
                    }
                    
                    exchange.Stop(true);
                }

                consoleMonitor.Stop();
            }
        }

        [TestMethod()]
        public void ReadfromPersistenceTest()
        {
            var dontCreateChannels = true;
            using (var actor = new Actor<Customer>())
            {
                var customer = new Customer(2);
                customer.Firstname = "John";
                customer.Lastname = "off yer Rocker mate";
                actor.PersistSelf(typeof(Customer), customer, new DefaultSerializer(Exchange.ControlChannelEncoding));
                actor.PersistanceSerializer = new DefaultSerializer(Exchange.ControlChannelEncoding);
                var returnedCustomer = actor.ReadfromPersistence(@"TestHelpers.Customer");
                Assert.AreEqual(customer.Firstname, returnedCustomer.Firstname);

                Assert.AreEqual(customer.Lastname, returnedCustomer.Lastname);
            }
        }

        [TestMethod()]
        public void WriteAndReadReadfromPersistenceTest()
        {
            using (var actor = new Actor<Order>())
            {
                var order = new Order();
                order.Description = "John's new order";
                actor.PersistSelf(typeof(Order), order, new DefaultSerializer(Exchange.ControlChannelEncoding));
                actor.PersistanceSerializer = new DefaultSerializer(Exchange.ControlChannelEncoding);
                var returnedOrder = actor.ReadfromPersistence(@"TestHelpers.Order");
                Assert.AreEqual(order.Description, returnedOrder.Description);
            }
        }

        [TestMethod]
        public void CreatingAnActor()
        {

            Type generic = typeof(Actor<>);

            Type[] typeArgs = { typeof(Order) };


            var obj = Activator.CreateInstance(typeof(Order));
            var constructed = generic.MakeGenericType(typeArgs);

            // Create a Type object representing the constructed generic 
            // type.
            
            using (var DaytonaContext = Context.Create(new ConsoleMonitor()))
            {
                var serializer = new DefaultSerializer(Exchange.ControlChannelEncoding);

                var target = (Actor)Activator.CreateInstance(constructed, DaytonaContext.NetMqContext, new BinarySerializer(), serializer);
                    
                obj = target.ReadfromPersistence(@"TestHelpers.Order", typeof(Order));

                ////not clear why I need to start the actor in a "create" test its not working anyway cant start an actor without setting up its channels
                ////which has not been done here
                //target.Start();
                target.Dispose(); //Actors have to be disposed of to release resources

            }

                //context.Terminate();
            
        }

        [TestMethod]
        public void CreatingAEntity()
        {

            Type typeInfo = typeof(Customer);
            Assert.IsTrue((typeInfo.GetConstructors().Any(c => c.GetParameters().Any(p => p.ParameterType == typeof(Actor)))));
            
            Type typeInfo2 = typeof(Order);
            Assert.IsFalse((typeInfo2.GetConstructors().Any(c => c.GetParameters().Any(p => p.ParameterType == typeof(Actor)))));

            Assert.IsTrue(typeInfo.BaseType == typeof(ActorFactory));
        }
    }
}
