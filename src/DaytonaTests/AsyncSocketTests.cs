using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Daytona;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Daytona.Tests
{
    using NetMQ;

    [TestClass()]
    public class AsyncSocketTests
    {
        [TestMethod()]
        public async Task getsomethingTest()
        {
            using (var context = NetMQContext.Create())
            {
                using (var actor = new Actor())
                {


                    //var socket = new AsyncSocket(context, actor, "");
                    //socket.Start();
                    //var message = await socket.ReceiveAsync();
                }
            }
        }
    }
}
