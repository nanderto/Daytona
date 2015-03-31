using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Daytona;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Daytona.Tests
{
    using System.Threading;

    using NetMQ;
    using NetMQ.Devices;

    using TestHelpers;

    [TestClass()]
    public class ExchangeTests
    {
        private static bool ReceiveReady = false;

        [TestMethod()]
        public void ExchangeTest()
        {
            using (var context = NetMQContext.Create())
            {
                var exchange = new XForwarder(
                    context,
                    Pipe.PublishAddressServer,
                    Pipe.SubscribeAddressServer,
                    DeviceMode.Threaded);
                //exchange.FrontendSetup.Subscribe("");
               // exchange.BackendSetup.Subscribe("");
                exchange.Start();
                var pub = Helper.GetConnectedPublishSocket(context, Pipe.PublishAddressClient);
                var sub = Helper.GetConnectedSubscribeSocket(context, Pipe.SubscribeAddressClient);
                var sub2 = Helper.GetConnectedSubscribeSocket(context, Pipe.SubscribeAddressClient);
                var sub3 = Helper.GetConnectedSubscribeSocket(context, Pipe.SubscribeAddressClient, "hello");
                var sub4 = Helper.GetConnectedSubscribeSocket(context, Pipe.SubscribeAddressClient, "Nothello");

                sub.ReceiveReady +=sub_ReceiveReady;

                Thread.Sleep(100);
                //while (!ReceiveReady)
                //{

                //}

                pub.Send("hello");
                var received = sub.ReceiveString();
                Assert.AreEqual("hello", received);
                var received2 = sub2.ReceiveString();
                Assert.AreEqual("hello", received2);

                var received3 = sub3.ReceiveString();
                Assert.AreEqual("hello", received3);

                pub.Send("Nothello");
                var received4 = sub4.ReceiveString();
                Assert.AreEqual("Nothello", received4);

                exchange.Stop(true);

                pub.Dispose();
                sub.Dispose();
                sub2.Dispose();
                sub3.Dispose();
                sub4.Dispose();
            }
        }

        private void sub_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            ReceiveReady = true;
        }

    }
}
