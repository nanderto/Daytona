﻿namespace Daytona
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    using NetMQ;

    public class Silo : IDisposable
    {
        /// <summary>
        /// This function is used to start and manage all of the running Actors. 
        /// It is set up on a listener that listens to all messages on the channel. 
        /// Each actor is checked if it does not exist it is started for the client
        /// </summary>
        public static Action<string, string, MethodInfo, List<object>, Actor> LaunchActors =
            (address, returnAddress, methodInfo, parameters, actor) =>
                {
                    object returnedObject = null;
                    List<RunningActors> runningActors = null;
                    string cleanAddress = address;

                    if (actor.PropertyBag.TryGetValue("RunningActors", out returnedObject))
                    {
                        runningActors = (List<RunningActors>)returnedObject;
                        var returnedActor = runningActors.FirstOrDefault(ra => ra.Address == address);

                        if (returnedActor == null)
                        {
                            //We dident find an actor, so we have to start one
                            StartNewActor(address, actor, methodInfo, parameters);

                            runningActors.Add(new RunningActors(address));
                        }
                        else
                        {
                            //"We found a running actor so er updated the time of the last heartbeat.");
                            returnedActor.LastHeartbeat = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        //"no collection of running actors, So I am creating one and starting a new runner");

                        runningActors = new List<RunningActors>();
                        StartNewActor(address, actor, methodInfo, parameters);
                        runningActors.Add(new RunningActors(address));
                        actor.PropertyBag.Add("RunningActors", runningActors);
                    }
                };

        public static Action<string, Actor> ShutDownAllActors = (instruction, actor) =>
            {
                object returnedObject = null;

                if (actor.PropertyBag.TryGetValue("RunningActors", out returnedObject))
                {
                    var runningActors = (List<RunningActors>)returnedObject;
                    foreach (var actr in runningActors)
                    {
                        actor.SendKillSignal(actor.Serializer, actor.OutputChannel, actr.Address);
                    }
                }
            };

        private readonly Dictionary<string, Entity> Entities = new Dictionary<string, Entity>();

        private readonly NetMQContext context;

        private BinarySerializer binarySerializer;

        private bool disposed;

        public Silo(NetMQContext context, BinarySerializer binarySerializer)
        {
            this.context = context;
            this.binarySerializer = binarySerializer;
            this.ActorFactory = new Actor(context, new BinarySerializer());
            this.ActorFactory.PersistanceSerializer = new DefaultSerializer(Pipe.ControlChannelEncoding);
            this.ConfigActorLauncher();

            //// need to add additional actors here. they will get configured and started in this constructor.
            //this.exceptionhandler();
        }

        /// <summary>
        /// Actor factory is an actor that is set up so it will not listen to any messages. 
        /// this is created to register and start sub-actors which perform the roles necessary for the Silo to function.
        /// the Sub actors will listen on there own channels for messages
        /// the output channel is alson set up, so the Actotfactory can (and is used) to send messages.
        /// </summary>
        public Actor ActorFactory { get; set; }

        public Exchange Exchange { get; set; }

        public static Silo Create()
        {
            var context = NetMQContext.Create();
            var exchange = new Exchange(context);
            exchange.Start();

            var silo = new Silo(context, new BinarySerializer()) { Exchange = exchange };
            return silo;
        }

        /// <summary>
        /// Sets up the correct registration for the function that runs and controls the Actors.
        /// </summary>
        public void ConfigActorLauncher()
        {
            var actions = new Dictionary<string, Delegate>();
            actions.Add("MethodInfo", LaunchActors);
            actions.Add("ShutDownAllActors", ShutDownAllActors);

            this.ActorFactory.RegisterActor(
                "ActorLauncher",
                string.Empty,
                "ActorLauncher outRoute",
                this.Entities,
                new BinarySerializer(),
                new DefaultSerializer(Exchange.ControlChannelEncoding),
                actions);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public Silo RegisterEntity(Type type)
        {
            // Type type = actor.GetType();
            this.Entities.Add(type.FullName, new Entity(type));
            return this;
        }

        public void Start()
        {
            this.ActorFactory.StartAllActors();
        }

        public void Stop()
        {
            //we should stop all active actors
            var netMqMessage = new NetMQMessage();
            var serializer = new BinarySerializer();
            netMqMessage.Append(new NetMQFrame(serializer.GetBuffer("Aslongasitissomething")));
            netMqMessage.Append(new NetMQFrame(serializer.GetBuffer("shutdownallactors")));
            this.ActorFactory.OutputChannel.SendMessage(netMqMessage);

            //need to stop the axtor launcher
            var netMqMessage2 = new NetMQMessage();
            netMqMessage2.Append(new NetMQFrame(serializer.GetBuffer("Aslongasitissomething")));
            netMqMessage2.Append(new NetMQFrame(serializer.GetBuffer("stop")));
            this.ActorFactory.OutputChannel.SendMessage(netMqMessage2);
        }

        private static void StartNewActor(string address, Actor actor, MethodInfo methodInfo, List<object> parameters)
        {
            string cleanAddress = address;
            string id;
            var addressAndId = address.Split('/');
            var addressWithOutId = addressAndId[0];
            if (addressAndId.Length > 1)
            {
                id = addressAndId[1];

                if (string.IsNullOrEmpty(id))
                {
                    // no number so creating a newone. need to figure that out
                    cleanAddress = addressWithOutId.Replace("/", "");
                }
            }

            Type generic = typeof(Actor<>);
            Entity entity = null;
            actor.Entities.TryGetValue(addressAndId[0], out entity);

            Type[] typeArgs = { entity.EntityType };

            var entityFromPersistence = actor.ReadfromPersistence(cleanAddress, entity.EntityType);

            if (entityFromPersistence == null)
            {
                entityFromPersistence = Activator.CreateInstance(entity.EntityType);
            }

            if (entity.EntityType.BaseType == typeof(ActorFactory))
            {
                ((ActorFactory)entityFromPersistence).Factory = actor;
            }

            //actor.PersistanceSerializer.Deserializer(Pipe.ControlChannelEncoding.GetBytes())
            // Create a Type object representing the constructed generic 
            // type.
            var constructed = generic.MakeGenericType(typeArgs);

            var target =
                (Actor)
                Activator.CreateInstance(
                    constructed,
                    actor.Context,
                    entityFromPersistence,
                    cleanAddress,
                    new BinarySerializer(),
                    new DefaultSerializer(Exchange.ControlChannelEncoding));
            target.Name = cleanAddress;
            var result = methodInfo.Invoke(entityFromPersistence, parameters.ToArray());
            var store = new Store(target.PersistanceSerializer);
            store.Persist(entity.EntityType, entityFromPersistence, cleanAddress);

            //var dataWriter = new DataWriterReader();
            //dataWriter.PersistSelf(clown.ClownType, clownFromPersistence, actor.PersistanceSerializer);

            Task.Run(
                () =>
                    {
                        target.Start();
                        target.Dispose();
                    });
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.ActorFactory.Dispose();
                    this.Exchange.Dispose();
                    this.context.Dispose();
                }

                //// There are no unmanaged resources to release, but
                //// if we add them, they need to be released here.
            }

            this.disposed = true;
        }
    }
}