using System;
using System.Collections.Generic;
using System.IO;
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
        public ZmqSocket OutputChannel;
        private bool disposed;
        public ISerializer Serializer;
        private ZmqSocket sendControlChannel;

        public string InRoute { get; set; }

        public string OutRoute { get; set; }

        public Delegate Workload { get; set; }
        public Delegate Callback { get; set; }
        public Action<IPayload, string, ZmqSocket, Actor> ExecuteAction { get; set; }
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

        public Actor(ZmqContext context, string inRoute, string outRoute, ISerializer serializer, Action<IPayload, string, string, ZmqSocket, Actor> workload)
        {
            this.Serializer = serializer;
            this.context = context;
            this.InRoute = inRoute;
            this.OutRoute = outRoute;
            this.Workload = workload;
            this.PropertyBag = new Dictionary<string, string>();
            SetUpReceivers(context, inRoute);
            SetUpOutputChannel(this.context);
        }

        public Actor(ZmqContext context, string inRoute, string outRoute, ISerializer serializer, Action<IPayload, string, string, ZmqSocket, Actor> workload, Action<IPayload, string, ZmqSocket, Actor> executeAction)
        {
            this.ExecuteAction = executeAction;
            this.Serializer = serializer;
            this.context = context;
            this.InRoute = inRoute;
            this.OutRoute = outRoute;
            this.Workload = workload;
            this.PropertyBag = new Dictionary<string, string>();
            SetUpReceivers(context, inRoute);
            SetUpOutputChannel(this.context);
        }

        public Actor(ZmqContext context, string inRoute, string outRoute, ISerializer serializer)
        {
            this.Serializer = serializer;
            this.context = context;
            this.InRoute = inRoute;
            this.OutRoute = outRoute;
            this.PropertyBag = new Dictionary<string, string>();
            SetUpReceivers(context, inRoute);
            SetUpOutputChannel(this.context);
        }

        public Actor(ZmqContext context, string outRoute, ISerializer serializer)
        {
            this.Serializer = serializer;
            this.context = context;
            this.OutRoute = outRoute;
            this.PropertyBag = new Dictionary<string, string>();
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
        /// Creates a Socket and connects it to a endpoint that is bound to a Pipe
        /// </summary>
        /// <param name="context"></param>
        void SetUpReceivers(ZmqContext context)
        {
            subscriber = context.CreateSocket(SocketType.SUB);
            subscriber.Connect(Pipe.SubscribeAddressClient);
            subscriber.Subscribe(InRoute, Encoding.Unicode);
            if(this.sendControlChannel == null)
            {
                this.sendControlChannel = context.CreateSocket(SocketType.REQ);
                this.sendControlChannel.Connect(Pipe.PubSubControlBackAddressClient);
            }
            this.sendControlChannel.Send("Actor ReceiverChannel connected, Listening on " + Pipe.SubscribeAddressClient + " for " + InRoute, Encoding.Unicode);
            var replySignal = this.sendControlChannel.Receive(Encoding.Unicode);
            //Actor.Writeline(replySignal);

        }

        private void SetUpReceivers(ZmqContext context, string route)
        {
            InRoute = route;
            SetUpReceivers(context);
        }

        void SetUpOutputChannel(ZmqContext context)
        {
            OutputChannel = context.CreateSocket(SocketType.PUB);
            OutputChannel.Connect(Pipe.PublishAddressClient);
            if(this.sendControlChannel == null)
            {
                this.sendControlChannel = context.CreateSocket(SocketType.REQ);
                this.sendControlChannel.Connect(Pipe.PubSubControlBackAddressClient);
            }
            this.sendControlChannel.Send("Actor OutputChannel connected, Sending on " + Pipe.PublishAddressClient, Encoding.Unicode);
            var replySignal = this.sendControlChannel.Receive(Encoding.Unicode);
            //Actor.Writeline(replySignal);
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
               
                if (frameContents.Count > 1)
                {
                    var address = frameContents[0];
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
            bool stop = false;
            while (stop == false)
            {
                string address = string.Empty;
                ZmqMessage zmqmessage = null;
                Writeline("Waiting for message");
                //this.sendControlChannel.Send("Waiting for message");
                T message = this.ReceiveMessage<T>(subscriber, out zmqmessage, out address, out stop, this.Serializer);
                Writeline("Received message");
                //this.sendControlChannel.Send("Received message");
                if (message != null)
                {

                    object[] parameters = new object[5];
                    parameters[0] = message;
                    parameters[1] = address;
                    parameters[2] = OutRoute;
                    parameters[3] = OutputChannel;
                    parameters[4] = this;
                    Workload.DynamicInvoke(parameters);
                }
            }
        }

        private T ReceiveMessage<T>(ZmqSocket subscriber, out ZmqMessage zmqMessage, out string address, out bool stopSignal, ISerializer serializer)
        {
            stopSignal = false;
            T result = default(T);
            ZmqMessage zmqOut = new ZmqMessage();
            bool hasMore = true;
            address = string.Empty;
            int i = 0;
            while (hasMore)
            {
                Frame frame = subscriber.ReceiveFrame();
                if (i == 0)
                {
                    address = serializer.GetString(frame.Buffer);
                }
                if (i == 1)
                {                  
                    string stopMessage = serializer.GetString(frame.Buffer);
                    Writeline("Message: " + stopMessage);
                    if (stopMessage.ToLower() == "stop")
                    {
                        Writeline("received stop");
                        stopSignal = true;
                    }
                    else 
                    {
                        result = serializer.Deserializer<T>(stopMessage);
                    }                                                  
                }
                i++;
                zmqOut.Append(new Frame(frame.Buffer));
                hasMore = subscriber.ReceiveMore;
            }


            zmqMessage = zmqOut;
            return result;
        }
        /// <summary>
        /// Create and start a new actor by invoking the Lambda registered with the name provided. THis new ctor is 
        /// created on its own thread
        /// </summary>
        /// <param name="Name">name of the actor</param>
        public void CreateNewActor(string name)
        {
            Action del;
            if (ActorTypes.TryGetValue(name, out del))
            {
                Task.Run(() =>
                {
                    del.DynamicInvoke();
                });
            }
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

        public Actor RegisterActor<T>(string name, string inRoute, string outRoute, ISerializer serializer, Action<IPayload, string, string, ZmqSocket, Actor> workload) where T : IPayload
        {
            ActorTypes.Add(name, () =>
            {
                using (var actor = new Actor(this.context, inRoute, outRoute, serializer, workload))
                {
                    actor.Start<T>();
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

        public void Execute<T>(T input)
        {
            this.ExecuteAction.DynamicInvoke(input);           
        }

        public void SendOneMessageOfType<T>(string address, T message, ISerializer serializer, ZmqSocket socket) where T : IPayload
        {
            ZmqMessage zmqMessage = new ZmqMessage();
            zmqMessage.Append(new Frame(serializer.Encoding.GetBytes(address)));
            zmqMessage.Append(new Frame(serializer.GetBuffer(message)));
            var replySignal = this.sendControlChannel.Receive(Encoding.Unicode);
            socket.SendMessage(zmqMessage);
            this.sendControlChannel.Send("Just sent message to " + address + " Message is: " + message, Encoding.Unicode);
            replySignal = this.sendControlChannel.Receive(Encoding.Unicode);
            Actor.Writeline(replySignal);
        }

        public int CallBack(IAsyncResult result)
        {
            return Id;
        }

        private static readonly object synchLock = new object();

        public static void Writeline(string line)
        {
            lock (synchLock)
            {
                FileInfo fi = new FileInfo(@"c:\dev\Actor.log");
                var stream = fi.AppendText();
                stream.WriteLine(line);
                stream.Flush();
                stream.Close();
            }
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


        public int Id { get; set; }
    }


     
}
