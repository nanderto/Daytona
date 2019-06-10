using Daytona;
using NetMQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiloTest
{
    class Tester
    {
        public Tester()
        {

        }

        public void DoSomething()
        {
            using (var netMqContext = NetMQContext.Create())
            {
                Exchange exc = null;
                var exchange = new Exchange(netMqContext);
                exchange.Start();
               // Assert.IsTrue(exchange.XForwarder.IsRunning);
                exchange.Stop(true);

                //using (var exchange = new Exchange(netMqContext))
                //{
                //    //exc = exchange;
                //    //Assert.IsNotNull(exc);
                //    exchange.Start();
                //    Assert.IsTrue(exchange.XForwarder.IsRunning);
                //}

                //Assert.IsNull(exc);
            }
        }
    }
}
