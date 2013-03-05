using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelpers;

namespace StoreTests
{
    [TestClass]
    public class ContextTests
    {
        [TestMethod]
        public void IsAContext()
        {
            Daytona.Store.Context context = new Daytona.Store.Context();
            Assert.IsInstanceOfType(context, typeof(Daytona.Store.Context));
        }

        [TestMethod]
        public void IsAScope()
        {
            Daytona.Store.Scope<Customer> connection = new Daytona.Store.Scope<Customer>();
            Assert.IsInstanceOfType(connection, typeof(Daytona.Store.Scope<Customer>));
        }

        [TestMethod]
        public void IsAConnection()
        {
            Daytona.Store.Connection connection = new Daytona.Store.Connection();
            Assert.IsInstanceOfType(connection, typeof(Daytona.Store.Connection));
        }

        [TestMethod]
        public void GetConnection()
        {
            Daytona.Store.Context context = new Daytona.Store.Context();
            using (Daytona.Store.Connection connection = new Daytona.Store.Connection())
            {
                Assert.IsInstanceOfType(connection, typeof(Daytona.Store.Connection));
            }
            
        }

        [TestMethod]
        public void SaveACustomer()
        {
            Daytona.Store.Context context = new Daytona.Store.Context();
            using (var connection = context.GetConnection<Customer>())
            {
                var customer = new Customer
                { 
                    Firstname = "John",
                    Lastname = "Lemon"
                };
                var task = connection.Save(customer);
                int Id = task.Result;
                Assert.AreEqual(1, Id);
            }
        }
    }
}
