using Microsoft.Isam.Esent.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroMQ;

namespace Daytona.Store
{
    /// <summary>
    /// Context for Database, only one should be created, it should live for the life of the application.
    /// It runs the framework components that are responsible for sending messages from senders to receivers.
    /// It also handles the access to the actual storage mechanism
    /// There are no checks in the code to ensure only one context is created.
    /// </summary>
    public class Context : IDisposable
    {
        private ZeroMQ.ZmqContext context;

        private Instance EsentInstance { get; set; }
        
        Pipe pipe;
        
        private bool disposed;

        public Context()
        {
            context = ZmqContext.Create();
            pipe = new Pipe(context);           
            EsentInstance = EsentInstanceService.Service.EsentInstance;
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

            actorFactory.RegisterActor<DBPayload<T>>("Writer", "Writer", "Sender", serializer, (IPayload message, byte[] messageAsBytes, string inRoute, string outRoute, ZmqSocket socket, Actor actor) =>
            {
                if (!actor.PropertyBag.ContainsKey("Count"))
                {
                    actor.PropertyBag.Add("Count", "0");
                }
                var count = int.Parse(actor.PropertyBag["Count"]);
                count++;
                actor.PropertyBag["Count"] = count.ToString();

                actor.WriteLineToMonitor("Got here in the writer");
                
                var writer = new Writer(this.EsentInstance);
                var dBPayload = (DBPayload<T>)message;
                
                int Id = writer.Save<T>(messageAsBytes, actor.Serializer);

                dBPayload.Id = count;

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
            connection.AddScope<T>(new Scope<T>(new Actor(context, "Sender", "Writer", serializer3, (IPayload message, byte[] messageAsBytes, string inRoute, string outRoute, ZmqSocket socket, Actor actor) =>
            {
                DBPayload<T> payload;
                try
                {
                    payload = (DBPayload<T>)message;
                    actor.CallBack(payload.Id, null, null);
                }
                catch (Exception ex)
                {
                    actor.CallBack(-1, null, ex);
                }                             
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
                    //if (context != null)
                    //{
                    //    context.Dispose();
                    //}
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }
            disposed = true;
        }
    }
}

