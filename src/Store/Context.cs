using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroMQ;

namespace Daytona.Store
{
    public class Context
    {
        private ZeroMQ.ZmqContext _Context;

        public Context()
        {
            _Context = ZmqContext.Create();
           
        }

        public Connection GetConnection<T>()
        {
            var connection = new Connection();

            return GetConnection<T>(connection);
        }

        public Connection GetConnection<T>(Connection connection)
        {
            ISerializer serializer = new Serializer(Encoding.UTF8);
            var actorFactory = new Actor(_Context);

            actorFactory.RegisterActor<DBPayload<T>>("Writer", "Writer", "Sender", serializer, (Message, InRoute, OutRoute, Socket, Actor) =>
            {
                var writer = new Writer();
                int Id = writer.Save((DBPayload<T>)Message);
                var dBPayload = new DBPayload<T>();
                dBPayload.Id = Id;
                Actor.SendOneMessageOfType<DBPayload<T>>(OutRoute, dBPayload, serializer, Socket);
            });

            ISerializer serializer2 = new Serializer(Encoding.UTF8);
            actorFactory.RegisterActor<DBPayload<T>>("Sender", "Sender", "Writer", serializer2, (Message, InRoute, OutRoute, Socket, Actor) =>
            {
                Actor.Id = ((DBPayload<T>)Message).Id;
                Actor.CallBack(null);
            });
            actorFactory.CreateNewActor("Sender");
            actorFactory.CreateNewActor("Writer");
            ISerializer serializer3 = new Serializer(Encoding.UTF8);
            connection.AddScope<T>(new Scope<T>(new Actor(_Context, "Sender", serializer3)));
            return connection;
        }
    }
}
