
namespace Daytona.Tests
{
    using Daytona;
    using DaytonaTests;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using ZeroMQ;
    using Customer = TestHelpers.Customer;

    [TestClass]
    public class ActorTests
    {
        [TestMethod]
        public void CreateActor()
        {
            using (var context = ZmqContext.Create())
            {
                var actorFactory = new Actor<ActorFactory>(context);
            }
        }

        [TestMethod]
        public void RegisterActorTest()
        {
            using (var context = ZmqContext.Create())
            {
                //ISerializer serializer = new Daytona.Store.Serializer(Encoding.Unicode);
                var actorFactory = new Actor<ActorFactory>(context);

                actorFactory.RegisterActor(new Customer());

            }
        }

        [TestMethod]
        public void RegisterZingleClassActorTest()
        {
            using (var context = ZmqContext.Create())
            {
                var actorFactory = new Actor<ActorFactory>(context);

                actorFactory.RegisterActor(new Customer());

            }
         }

        //[TestMethod]
        //public void ClearHolderTest()
        //{
        //    using (var context = ZmqContext.Create())
        //    {
        //        var actor = new Actor(context);
        //        var account = (IAccount)actor.CreateInstance<IAccount, Account>();
        //        var newName = string.Format("{0}, {1}", "wilson", "Brad");
        //        Assert.IsInstanceOfType(account, typeof(IAccount));
        //        account.ClearHolder();
        //        account.UpdateHolder(newName);
        //    }
        //}

        [TestMethod]
        public void RegisterAccount()
        {
            //using (var context = ZmqContext.Create())
            //{
            //    ISerializer serializer = new Daytona.Store.Serializer(Encoding.Unicode);
            //    var actorFactory = new Actor(context);
            //    actorFactory.RegisterActor<DBPayload<Customer>>(
            //        serializer,
            //        (IPayload message, byte[] messageAsBytes, Actor actor) =>
            //            {
            //                var payload = (DBPayload<Customer>)message;
            //                var account = (IAccount)actor.CreateInstance<IAccount>();
            //                var newName = string.Format("{0}, {1}", "wilson", "Brad");
            //                Assert.IsInstanceOfType(account, typeof(IAccount));
            //                account.ClearHolder();
            //                account.UpdateHolder(newName);
            //            });

            //    actorFactory.StartAllActors();
            //}
        }
    }
}
