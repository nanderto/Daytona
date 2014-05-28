using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Daytona.Store;
using System.Text;
using TestHelpers;
namespace Daytona.Store.Tests
{
    using Microsoft.Isam.Esent.Interop;

    [TestClass]
    public class WriterTests
    {
        [TestMethod]
        public void SaveTest()
        {
           // var uniqueName = Guid.NewGuid().ToString().Replace("-", "");
            var storename = CleanupName(typeof(Customer).ToString());
            EsentConfig.CreateDatabaseAndActorStore(storename);
            Assert.IsTrue(EsentConfig.DoesDatabaseExist(EsentConfig.DatabaseName));
            var x = Environment.CurrentDirectory;
            Assert.IsTrue(EsentConfig.DoesStoreExist(storename));

            //var context = new Context();
            using (var esentInstance = new Instance("Instance"))
            {
                esentInstance.Parameters.CircularLog = true;
                esentInstance.Init();
                using (var session = new Session(esentInstance))
                {
                    Api.JetAttachDatabase(session, EsentConfig.DatabaseName, AttachDatabaseGrbit.None);
                }

                var writer = new Writer(esentInstance);

                var result = writer.Save<Customer>(Encoding.Unicode.GetBytes("this is my message"));

                result = writer.Save<Customer>(Encoding.Unicode.GetBytes("this is my message1"));

                result = writer.Save<Customer>(Encoding.Unicode.GetBytes("this is my message2"));
                Assert.AreNotEqual(result, 23);
            }
        }

        private static string CleanupName(string dirtyname)
        {
            return dirtyname.ToString().Replace("{", string.Empty).Replace("}", string.Empty).Replace("_", string.Empty).Replace(".", string.Empty);
        }  
    }
}
