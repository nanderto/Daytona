using Daytona;
using NetMQ;
using System;
using System.Threading;
using Xunit;

namespace XDaytonaTests
{
    public class ExchangeTests
    {
        [Fact]
        public void Create_Start_Stop_ShutDown_Exchange()
        {
            using (var netMqContext = NetMQContext.Create())
            {
                Exchange exc = null;
                using (var exchange = new Exchange(netMqContext))
                {
                    exc = exchange;
                    Assert.NotNull(exc);
                    exchange.Start();
                }

                Assert.NotNull(netMqContext);
            }
        }
    }
}
