// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Silo.cs" company="Brookfield Global Relocation Services">
// Copyright © 2014 All Rights Reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace Daytona
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    using NetMQ;
    using NetMQ.zmq;

    public class Silo : IDisposable
    {
        private readonly Dictionary<string, Clown> Clowns = new Dictionary<string, Clown>();

        private BinarySerializer binarySerializer;

        private NetMQContext context;

        private bool disposed;

        public Silo(NetMQContext context, BinarySerializer binarySerializer)
        {
            this.context = context;
            this.binarySerializer = binarySerializer;
            this.ActorFactory = new Actor(context, new BinarySerializer(), false);
            this.ActorFactory.PersistanceSerializer = new DefaultSerializer(Pipe.ControlChannelEncoding);
            this.ConfigActorLauncher();
            //// need to add additional actors here. they will get configured and started in this constructor.
            //this.exceptionhandler();
        }

        /// <summary>
        /// Actor factory is an actor that is set up so it will not isten to any messages. 
        /// this is created to register and start sub-actors which perform the roles necessary for the Silo to function.
        /// the Sub actors will listen on there own channels for messages
        /// </summary>
        public Actor ActorFactory { get; set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Start()
        {
            this.ActorFactory.StartAllActors();
        }

        public void ConfigActorLauncher()
        {
            this.ActorFactory.RegisterActor(
                           "ActorLauncher",
                           string.Empty,
                           "ActorLauncher outRoute",
                           this.Clowns,
                           new BinarySerializer(),
                           new DefaultSerializer(Exchange.ControlChannelEncoding),
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

                                   //"We found a running actor so er did nothing");
                               }
                               else
                               {
                                   //"no collection of running actors, So I am creating one and starting a new runner");
                                   
                                   runningActors = new List<RunningActors>();
                                   StartNewActor(address, actor, methodInfo, parameters);
                                   runningActors.Add(new RunningActors(address));
                                   actor.PropertyBag.Add("RunningActors", runningActors);
                               }
                           });
        }

        private static void StartNewActor(string address, Actor actor, MethodInfo methodInfo, List<object> parameters)
        {
            string cleanAddress = address;
            var addressAndId = address.Split('/');
            var addressWithOutId = addressAndId[0];
            var id = addressAndId[1];

            if (string.IsNullOrEmpty(addressAndId[1]))
            {
                // no number so creating a newone. need to figure that out
                cleanAddress = addressWithOutId.Replace("/", "");
            }

            Type generic = typeof(Actor<>);
            Clown clown = null;
            actor.Clowns.TryGetValue(addressAndId[0], out clown);

            Type[] typeArgs = { clown.ClownType };

            var clownFromPersistence = actor.ReadfromPersistence(cleanAddress, clown.ClownType);
            if (clownFromPersistence == null)
            {
                clownFromPersistence = Activator.CreateInstance(clown.ClownType);
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
                    clownFromPersistence,
                    cleanAddress,
                    new BinarySerializer(),
                    new DefaultSerializer(Exchange.ControlChannelEncoding));
            var result = methodInfo.Invoke(clownFromPersistence, parameters.ToArray());
            var dataWriter = new DataWriterReader();
            dataWriter.PersistSelf(clown.ClownType, clownFromPersistence, actor.PersistanceSerializer);
            
            Task.Run(() => target.Start());
        }

        public Silo RegisterClown(Type type)
        {
            // Type type = actor.GetType();
            this.Clowns.Add(type.FullName, new Clown(type));
            return this;
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.ActorFactory.Dispose();
                }

                //// There are no unmanaged resources to release, but
                //// if we add them, they need to be released here.
            }

            this.disposed = true;
        }
    }
}