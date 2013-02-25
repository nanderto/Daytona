using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroMQ;

namespace Daytona
{
    public class Actor : IDisposable
    {
        private ZmqContext context;
        private IPayload payload;
        private ZmqSocket subscriber;
        private ZmqSocket OutputChannel;
        private bool disposed;
        private ISerializer serializer;
        public string InRoute { get; set; }
        public string OutRoute { get; set; }
        public Delegate Workload { get; set; }
        private Dictionary<string, Action> ActorTypes = new Dictionary<string, Action>();

        public Dictionary<string, string> PropertyBag { get; set; }
        
        public Actor(ZmqContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Use this constructor when the actor does not need to send messages to other actors.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="route"></param>
        /// <param name="workload"></param>
        public Actor(ZmqContext context, string route, Action<string, string> workload)
        {
            this.context = context;
            this.InRoute = route;
            this.Workload = workload;
            SetUpReceivers(context, route);
        }

        /// <summary>
        /// Use this constructor when the actor needs to send messages to other actors
        /// </summary>
        /// <param name="context">The ZmqContext for creating message channels</param>
        /// <param name="inRoute">the input address that this actor will listen to</param>
        /// <param name="OutRoute">the address that the actor will send messages to, Currently a little limited because we should be able to send to any address</param>
        /// <param name="workload">the Lambda expression that is the work that this Actor does. this expression Must be single threaded</param>
        public Actor(ZmqContext context, string inRoute, string OutRoute, Action<string, string, string, ZmqSocket> workload)
        {
            this.OutRoute = OutRoute;
            this.context = context;
            this.InRoute = inRoute;
            this.Workload = workload;
            SetUpReceivers(context, inRoute);
            SetUpOutputChannel(context);
        }

        /// <summary>
        /// Use this constructor when the actor needs to send messages to other actors, and it needs data from the actor work with.
        /// </summary>
        /// <param name="context">The ZmqContext for creating message channels</param>
        /// <param name="inRoute">the input address that this actor will listen to</param>
        /// <param name="OutRoute">the address that the actor will send messages to, Currently a little limited because we should be able to send to any address</param>
        /// <param name="workload">the Lambda expression that is the work that this Actor does. this expression Must be single threaded. In this case the Lambda has access
        /// the Actor and data contained within the Actor</param>
        public Actor(ZmqContext context, string inRoute, string outRoute, Action<string, string, string, ZmqSocket, Actor> workload)
        {
            this.context = context;
            this.InRoute = inRoute;
            this.OutRoute = outRoute;
            this.Workload = workload;
            this.PropertyBag = new Dictionary<string, string>();
            SetUpReceivers(context, inRoute);
            SetUpOutputChannel(this.context);
        }

        public Actor(ZmqContext context, string inRoute, string outRoute, IPayload payload, ISerializer serializer, Action<IPayload, string, string, ZmqSocket, Actor> workload)
        {
            this.serializer = serializer;
            this.payload = payload;
            this.context = context;
            this.InRoute = inRoute;
            this.OutRoute = outRoute;
            this.Workload = workload;
            this.PropertyBag = new Dictionary<string, string>();
            SetUpReceivers(context, inRoute);
            SetUpOutputChannel(this.context);
        }

        public Actor(ZmqContext context, string inRoute, Action<Actor> workload)
        {
            this.context = context;
            this.InRoute = inRoute;
            this.Workload = workload;
            this.PropertyBag = new Dictionary<string, string>();
            SetUpReceivers(context, inRoute);
            SetUpOutputChannel(this.context);
        }
        /// <summary>
        /// Creates a Socket and connects it to a endpoint that is bound to a Pibe
        /// </summary>
        /// <param name="context"></param>
        void SetUpReceivers(ZmqContext context)
        {
            subscriber = context.CreateSocket(SocketType.SUB);
            subscriber.Connect("tcp://localhost:5555");
            subscriber.Subscribe(InRoute, Encoding.Unicode);
            //subscriber.SubscribeAll();
        }

        private void SetUpReceivers(ZmqContext context, string route)
        {
            InRoute = route;
            SetUpReceivers(context);
        }

        void SetUpOutputChannel(ZmqContext context)
        {
            OutputChannel = context.CreateSocket(SocketType.PUB);
            OutputChannel.Connect("tcp://localhost:5556");
        }

        /// <summary>
        /// Start is called on all actors to have them listen for messages, they will receive and process one message 
        /// at a time
        /// </summary>
        public void Start()
        {
            while (true)
            {
                //string message = subscriber.Receive(Encoding.Unicode);
                var zmqmessage = subscriber.ReceiveMessage();
                var frameContents = zmqmessage.Select(f => Encoding.Unicode.GetString(f.Buffer)).ToList();
                                   //var message = zmqmessage.;
                var Address = frameContents[0];
                if (frameContents.Count > 1)
                {
                    var message = frameContents[1];

                    if (message != null)
                    {
                        if (string.IsNullOrEmpty(OutRoute))
                        {
                            object[] Params = new object[2];
                            Params[0] = message;
                            Params[1] = InRoute;
                            Workload.DynamicInvoke(Params);
                        }
                        else
                        {
                            if (PropertyBag != null)
                            {
                                object[] Params = new object[5];
                                Params[0] = message;
                                Params[1] = InRoute;
                                Params[2] = OutRoute;
                                Params[3] = OutputChannel;
                                Params[4] = this;
                                Workload.DynamicInvoke(Params);
                            }
                            else
                            {
                                object[] Params = new object[4];
                                Params[0] = message;
                                Params[1] = InRoute;
                                Params[2] = OutRoute;
                                Params[3] = OutputChannel;
                                Workload.DynamicInvoke(Params);
                            }
                        }
                    }
                }
            }
        }

        public void Start<T>() where T : IPayload
        {
            while (true)
            {
                string address = string.Empty;
                ZmqMessage zmqmessage = null;
                T message = this.ReceiveMessage<T>(subscriber, out zmqmessage, out address, this.serializer);

                object[] Params = new object[5];
                Params[0] = message;
                Params[1] = address;
                Params[2] = OutRoute;
                Params[3] = OutputChannel;
                Params[4] = this;
                Workload.DynamicInvoke(Params);
            }
        }

        private T ReceiveMessage<T>(ZmqSocket Subscriber, out ZmqMessage zmqMessage, out string address, ISerializer serializer)
        {
            T result = default(T);
            ZmqMessage zmqOut = new ZmqMessage();
            bool hasMore = true;
            string message = "";
            address = string.Empty;
            int i = 0;
            while (hasMore)
            {
                message = Subscriber.Receive(Encoding.Unicode);
                if (i == 0)
                {
                    address = message;
                }
                if (i == 1)
                {
                    result = (T)serializer.Deserializer<T>(message);
                }

                i++;
                zmqOut.Append(new Frame(Encoding.Unicode.GetBytes(message)));
                hasMore = Subscriber.ReceiveMore;
            }

            zmqMessage = zmqOut;
            return result;
        }
        /// <summary>
        /// Create and start a new actor by invoking the Lambda registered with the name provided. THis new ctor is 
        /// created on its own thread
        /// </summary>
        /// <param name="Name">name of the actor</param>
        public void CreateNewActor(string Name)
        {
            Action Del;
            bool result = ActorTypes.TryGetValue(Name, out Del);

            Task.Run(() =>
            {
                Del.DynamicInvoke();
            });
        }

        /// <summary>
        /// Create and start all actors that are registered in the collection of subActors. this 
        /// is done by invoking the Lambda registered in the collection.
        /// Each actor is started on its own thread
        /// </summary>
        public void StartAllActors()
        {
            foreach (var item in ActorTypes)
            {
                Task.Run(() =>
                {
                    item.Value.DynamicInvoke();
                });
            }
        }

        /// <summary>
        /// Registor a Sub-Actor within this actor
        /// </summary>
        /// <param name="name">Name of Sub actor</param>
        /// <param name="inRoute">Address that this actor will respond to</param>
        /// <param name="outRoute">Address that this actor send its output messages to</param>
        /// <param name="workload"></param>
        /// <returns></returns>
        public Actor RegisterActor(string name, string inRoute, string outRoute, Action<string, string, string, ZmqSocket> workload)
        {
            ActorTypes.Add(name, () =>
            {
                using (var actor = new Actor(this.context, inRoute, outRoute, workload))
                {
                    actor.Start();
                }
            });
            return this;
        }

        public Actor RegisterActor(string name, String route, Action<string, string> workload)
        {
            ActorTypes.Add(name, () =>
            {
                using (var actor = new Actor(this.context, route, workload))
                {
                    actor.Start();
                }
            });
            return this;
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
                    if (subscriber != null)
                    {
                        subscriber.Dispose();
                    }
                    if (OutputChannel != null)
                    {
                        OutputChannel.Dispose();
                    }
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }
            disposed = true;
        }        
    }


     
}
