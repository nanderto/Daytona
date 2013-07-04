//-----------------------------------------------------------------------
// <copyright file="Context.cs" company="The Phantom Coder">
//     Copyright The Phantom Coder. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Daytona.Store
{
    using Microsoft.Isam.Esent.Interop;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.Threading;
    using ZeroMQ;

    /// <summary>
    /// Context for Database, only one should be created, it should live for the life of the application.
    /// It runs the framework components that are responsible for sending messages from senders to receivers.
    /// It also handles the access to the actual storage mechanism
    /// There are no checks in the code to ensure only one context is created.
    /// </summary>
    public class Context : IDisposable
    {
        private static readonly object synclock = new object();

        private readonly ZeroMQ.ZmqContext context;

        private bool disposed;

        private bool isStoreConfigured;

        private string longStoreName;

        private Pipe pipe;

        private Dictionary<string, bool>  tableDoesExist = new Dictionary<string, bool>();

        public Context()
        {
            context = ZmqContext.Create();
            SetUpOutputChannel(context);
            pipe = new Pipe(context);
            ConfigureEsentDatabase();
            this.EsentInstance = GetEsentInstance();
        }

        private Instance EsentInstance { get; set; }

        public bool ConfigureStore()
        {
            if (!EsentConfig.DoesDatabaseExist(EsentConfig.DatabaseName))
            {
                EsentConfig.CreateDatabase();
            }

            return true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public Connection GetConnection<T>()
        {
            var storeName = CleanupName(typeof(T).ToString());
            bool exists = false;
            if (!this.tableDoesExist.TryGetValue(storeName, out exists))
            {
                if (!EsentConfig.DoesStoreExist(storeName, this.EsentInstance))
                {
                    EsentConfig.CreateMessageStore(storeName, this.EsentInstance);
                    this.tableDoesExist.Add(storeName, true);
                }
            }

            var connection = new Connection(context);
            return GetConnection<T>(connection);
        }

        private void SetUpOutputChannel(ZmqContext context)
        {
            this.OutputChannel = context.CreateSocket(SocketType.PUB);
            this.OutputChannel.Connect(Pipe.PublishAddressClient);
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

        private static string CleanupName(string dirtyname)
        {
            return dirtyname.ToString().Replace("{", string.Empty).Replace("}", string.Empty).Replace("_", string.Empty).Replace(".", string.Empty);
        }

        private void ConfigureEsentDatabase()
        {
            bool esentTempPathInUseExceptionTrue = false;
            int retryCount = 0;
            do
            {
                esentTempPathInUseExceptionTrue = false;
                try
                {
                    if (!isStoreConfigured)
                    {
                        lock (synclock)
                        {
                            if (!isStoreConfigured)
                            {
                                isStoreConfigured = this.ConfigureStore();
                            }
                        }
                    }
                }
                catch (EsentTempPathInUseException)
                {
                    Trace.WriteLine("Path in use exception" + retryCount.ToString(CultureInfo.CurrentCulture));
                    esentTempPathInUseExceptionTrue = true;
                    ++retryCount;
                    if (retryCount > 4)
                    {
                        throw;
                    }

                    Thread.Sleep(retryCount * retryCount * 1000);
                }
            }
            while (esentTempPathInUseExceptionTrue);
        }

        private static void SendMessage(string address, string message, ISerializer serializer, ZmqSocket socket)
        {
            ZmqMessage zmqMessage = new ZmqMessage();
            zmqMessage.Append(new Frame(serializer.Encoding.GetBytes(address)));
            zmqMessage.Append(new Frame(serializer.Encoding.GetBytes(message)));
            socket.SendMessage(zmqMessage);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {                   
                    ISerializer serializer = new Serializer(Encoding.UTF8);
                    //ISerializer serializer2 = new Serializer(Encoding.UTF8);
                    SendMessage("Writer", "stop", serializer, this.OutputChannel);
                    //SendMessage("Sender", "stop", serializer2, this.OutputChannel);
                    

                    if (this.EsentInstance != null)
                    {
                        this.EsentInstance.Dispose();
                    }

                    pipe.Exit();

                    if (this.OutputChannel != null)
                    {
                        this.OutputChannel.Linger = new TimeSpan(0, 0, 0, 0, 500);
                        this.OutputChannel.Close();
                        this.OutputChannel.Dispose();
                    }

                    if (context != null)
                    {
                        context.Terminate();
                        context.Dispose();
                    }
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }
            disposed = true;
        }

        //private readonly string name = EsentConfig.DatabaseName;
        private Instance GetEsentInstance()
        {
            var esentInstance = new Instance("Instance");
            esentInstance.Parameters.CircularLog = true;
            esentInstance.Init();
            using (var session = new Session(esentInstance))
            {
                Api.JetAttachDatabase(session, EsentConfig.DatabaseName, AttachDatabaseGrbit.None);
            }
            return esentInstance;
        }

        public ZmqSocket OutputChannel { get; set; }

        internal void SendMessage(string p1, string p2)
        {
            throw new NotImplementedException();
        }
    }
}