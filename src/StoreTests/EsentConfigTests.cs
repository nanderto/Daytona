using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Daytona.Store;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Daytona.Store.Tests
{
    [TestClass()]
    public class EsentConfigTests
    {
        [TestMethod()]
        public void DoesStoreExistTest_Doesnt_Exist()
        {
            var uniqueName = new Guid().ToString();
            Assert.IsFalse(EsentConfig.DoesDatabaseExist(uniqueName));
        }

        [TestMethod()]
        public void CreateDatabaseTest()
        {
            var uniqueName =  Guid.NewGuid().ToString().Replace("-", "");
            EsentConfig.CreateDatabaseAndActorStore(uniqueName);
            Assert.IsTrue(EsentConfig.DoesDatabaseExist(EsentConfig.DatabaseName));
            var x = Environment.CurrentDirectory;
            Assert.IsTrue(EsentConfig.DoesStoreExist(uniqueName));
        }

        [TestMethod()]
        public void SaveActorTest()
        {
            var uniqueName = Guid.NewGuid().ToString().Replace("-", "");
            EsentConfig.CreateDatabaseAndActorStore(uniqueName);
            Assert.IsTrue(EsentConfig.DoesDatabaseExist(EsentConfig.DatabaseName));
            var x = Environment.CurrentDirectory;
            Assert.IsTrue(EsentConfig.DoesStoreExist(uniqueName));

        }
    }
}
