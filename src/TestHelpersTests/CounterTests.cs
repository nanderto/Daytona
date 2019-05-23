using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace TestHelpers.Tests
{
    using System.Threading;

    using Daytona;

    [TestClass()]
    public class CounterTests
    {
        private static ManualResetEvent mRE = new ManualResetEvent(false);
        [TestMethod()]
        public void AddTest()
        {
            var counter = new Counter(Guid.NewGuid());
            var initialCount = counter.TheCount;

            counter.Add();

            Assert.AreEqual(initialCount + 1, counter.TheCount);
        }

        [TestMethod]
        public void SpawnOneProcess()
        {
            var manualResetEvent = new ManualResetEvent(false);

            using (var context = Context.Create())
            {
                var actorReference = context.Spawn("Reader",
                    (message, sender, actor) =>
                        {                     
                            if (message as string == "read")
                            {
                                Console.WriteLine("yeah");
                            }
                            manualResetEvent.Set();
                        });
                Thread.Sleep(300);
                //for (int i = 0; i < 10; i++)
                //{
                    actorReference.Tell("read");
                    manualResetEvent.WaitOne();
                    manualResetEvent.Reset();
                //}
                
                //do
                //{
                //    Thread.Sleep(30);
                //}
                //while (!interrupted);

                actorReference.Kill();
            }

        }
    }
}
