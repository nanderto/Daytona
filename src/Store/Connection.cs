namespace Daytona.Store
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using ZeroMQ;

    /// <summary>
    /// Connection provides access to data persistence
    /// </summary>
    public class Connection : IDisposable
    {
        private readonly Dictionary<string, IScope> scopes = new Dictionary<string, IScope>();
        
        private bool disposed;

        private ZmqContext zmqContext = null;

        public Connection(ZmqContext zmqContext)
        {
            this.zmqContext = zmqContext;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task<int> Save<T>(T input) where T : IPayload
        {
            IScope scope = null;
            this.scopes.TryGetValue(typeof(T).Name, out scope);
            //Scope<T> s = (Scope<T>)scope;
            var result = scope.SaveAsync<T>(input);

            return await result;
        }

        /// <summary>
        /// Adding a newscope that defines the access
        /// </summary>
        /// <typeparam name="T">THe type of object to be persisted</typeparam>
        /// <param name="scope">the scope parameter of the specified type</param>
        internal void AddScope<T>(Scope<T> scope)
        {
            this.scopes.Add(typeof(T).Name, (IScope)scope);
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
            disposed = false;
            if (!disposed)
            {
                if (disposing)
                {
                    foreach (var item in this.scopes)
                    {
                        if (item.Value.actor.IsRunning)
                        {
                            var OutputChannel = zmqContext.CreateSocket(SocketType.PUB);
                            OutputChannel.Connect(Pipe.PublishAddressClient);
                            ISerializer serializer = new Serializer(Encoding.UTF8);
                            SendMessage ("Sender", "stop", serializer, OutputChannel);

                            while (item.Value.actor.IsRunning == true)
                            {
                                string a = "base";
                            }
                        }
                    }
                    
                    //if (subscriber != null)
                    //{
                    //    subscriber.Dispose();
                    //}
                    //if (OutputChannel != null)
                    //{
                    //    OutputChannel.Dispose();
                    //}
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }

            disposed = true;
        }
    }
}