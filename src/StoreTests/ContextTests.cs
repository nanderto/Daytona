using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelpers;
using ZeroMQ;
using Daytona;
using System.Threading.Tasks;

namespace StoreTests
{
    [TestClass]
    public class ContextTests
    {
        [TestMethod, TestCategory("Unit")]
        public void IsAContext()
        {
            using (Daytona.Store.Context context = new Daytona.Store.Context())
            {
                Assert.IsInstanceOfType(context, typeof(Daytona.Store.Context));
            }
        }

        [TestMethod, TestCategory("Unit")]
        public void IsAScope()
        {
            Daytona.Store.Scope<Customer> connection = new Daytona.Store.Scope<Customer>();
            Assert.IsInstanceOfType(connection, typeof(Daytona.Store.Scope<Customer>));
        }

        [TestMethod, TestCategory("Unit")]
        public void IsAConnection()
        {
            ZmqContext zmqContext = ZmqContext.Create();
            Daytona.Store.Connection connection = new Daytona.Store.Connection(zmqContext);
            Assert.IsInstanceOfType(connection, typeof(Daytona.Store.Connection));
        }

        [TestMethod, TestCategory("Unit")]
        public void GetConnection()
        {
            var task = Task.Run(() =>
            {
                using (Daytona.Store.Context context = new Daytona.Store.Context())
                {
                    using (var connection = context.GetConnection<Customer>())
                    {
                        Assert.IsInstanceOfType(connection, typeof(Daytona.Store.Connection));
                    }
                }
            });

            task.Wait();
        }

        [TestMethod]
        public void SaveACustomer()
        {
            using (Daytona.Store.Context context = new Daytona.Store.Context())
            {
                using (var connection = context.GetConnection<Customer>())
                {
                    var customer = new Customer
                    {
                        Firstname = "John",
                        Lastname = "Lemon"
                    };
                    var task = connection.Save(customer);
                    int id = task.Result;
                    Assert.AreEqual(1, id);
                }
            }
        }
    }
}
