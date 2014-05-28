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

    using TestHelpers;

    using ZeroMQ;

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
    }
}
