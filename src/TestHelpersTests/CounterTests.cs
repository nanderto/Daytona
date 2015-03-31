using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace TestHelpers.Tests
{
    [TestClass()]
    public class CounterTests
    {
        [TestMethod()]
        public void AddTest()
        {
            var counter = new Counter(Guid.NewGuid());
            var initialCount = counter.TheCount;

            counter.Add();

            Assert.AreEqual(initialCount + 1, counter.TheCount);
        }
    }
}
