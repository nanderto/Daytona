﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroMQ;

namespace Daytona.Store
{
    public class Context : IDisposable
    {
        private ZeroMQ.ZmqContext context;

        Pipe pipe;
        private bool disposed;

        public Context()
        {
            context = ZmqContext.Create();
            pipe = new Pipe(context);
        }

        public Connection GetConnection<T>()
        {
            var connection = new Connection();

            return GetConnection<T>(connection);
        }

        public Connection GetConnection<T>(Connection connection)
        {
            ISerializer serializer = new Serializer(Encoding.UTF8);
            var actorFactory = new Actor(context);

            actorFactory.RegisterActor<DBPayload<T>>("Writer", "Writer", "Sender", serializer, (message, inRoute, outRoute, socket, actor) =>
            {
                Actor.Writeline("Got here in the writer");
                var writer = new Writer();
                int Id = writer.Save((DBPayload<T>)message);
                var dBPayload = new DBPayload<T>();
                dBPayload.Id = Id;
                actor.SendOneMessageOfType<DBPayload<T>>(outRoute, dBPayload, serializer, socket);
            });

            //ISerializer serializer2 = new Serializer(Encoding.UTF8);
            //actorFactory.RegisterActor<DBPayload<T>>("Sender", "Sender", "Writer", serializer2, (Message, InRoute, OutRoute, Socket, Actor) =>
            //{
            //    Actor.Id = ((DBPayload<T>)Message).Id;
            //    Actor.CallBack(null);
            //});
            //actorFactory.CreateNewActor("Sender");
            actorFactory.CreateNewActor("Writer");
            ISerializer serializer3 = new Serializer(Encoding.UTF8);
            connection.AddScope<T>(new Scope<T>(new Actor(context, "Sender", "Writer", serializer3,(message, inRoute, outRoute, socket, actor) =>
                {
                    actor.CallBack(null);
                })));
            return connection;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    pipe.Exit();
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }
            disposed = true;
        }
    }
}
