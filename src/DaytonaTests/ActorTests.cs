using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Daytona;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Daytona.Tests
{
    using Daytona.Store;

    using DaytonaTests;

    using ZeroMQ;

    using Customer = TestHelpers.Customer;

    [TestClass]
    public class ActorTests
    {
        [TestMethod]
        public void RegisterActorTest()
        {
            using (var context = ZmqContext.Create())
            {
                ISerializer serializer = new Daytona.Store.Serializer(Encoding.Unicode);
                var actorFactory = new Actor(context);

                actorFactory.RegisterActor<DBPayload<Customer>>("Writer", "Writer", "Sender", serializer, (IPayload message, byte[] messageAsBytes, string inRoute, string outRoute, ZmqSocket socket, Actor actor) =>
                {

                });

            }
        }

        [TestMethod]
        public void RegisterZingleClassActorTest()
        {
            using (var context = ZmqContext.Create())
            {
                ISerializer serializer = new Daytona.Store.Serializer(Encoding.Unicode);
                var actorFactory = new Actor(context);

                actorFactory.RegisterActor<Customer>(serializer, (IPayload message, byte[] messageAsBytes, Actor actor) =>
                {

                });

            }
         }

        [TestMethod]
        public void RegisterAccount()
        {
            using (var context = ZmqContext.Create())
            {
            //    ISerializer serializer = new Daytona.Store.Serializer(Encoding.Unicode);
                var actor = new Actor(context);
            //    actorFactory.RegisterActor<DBPayload<Customer>>(serializer,
            //        (IPayload message, byte[] messageAsBytes, Actor actor) =>
            //            {
                            //var payload = (DBPayload<Customer>)message;
                            var account = (IAccount) actor.CreateInstance<Account>();
                var newName = string.Format("{0}, {1}", "wilson", "Brad");
                            account.UpdateHolder(newName);
                        //});

            //    actorFactory.StartAllActors();
            }
        }


    }
}
