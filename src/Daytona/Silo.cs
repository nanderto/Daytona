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
                           (address, methodInfo, parameters, clowns, actor) =>
                           {
                               object returnedObject = null;
                               List<RunningActors> runningActors = null;

                               if (actor.PropertyBag.TryGetValue("RunningActors", out returnedObject))
                               {
                                   runningActors = (List<RunningActors>)returnedObject;
                                   var returnedActor = runningActors.FirstOrDefault(ra => ra.Address == address);

                                   if (returnedActor == null)
                                   {
                                       //Console.WriteLine("We dident find an actor");
                                       var addressAndNumber = address.Split('/');
                                       
                                       Type generic = typeof(Actor<>);

                                       var type = Type.GetType(addressAndNumber[0]);
                                       Type[] typeArgs = { type };

                                       // Create a Type object representing the constructed generic 
                                       // type.
                                       Type constructed = generic.MakeGenericType(typeArgs);


                                       //var customer = new Actor<type>(actor.Context, new BinarySerializer());

                                       if (addressAndNumber[0] == "TestHelpers.Customer")
                                       {
                                           //var customer = new Actor<Customer>(actor.Context, new BinarySerializer());
                                           //customer.StartWithIdAndMethod(address, methodInfo, parameters);
                                       }

                                       if (addressAndNumber[0] == "TestHelpers.Order")
                                       {
                                           //var order = new Actor<Order>(actor.Context, new BinarySerializer());
                                           //order.StartWithIdAndMethod(address, methodInfo, parameters);
                                       }

                                       Console.WriteLine("I wish I could start a method");
                                       ////start actor
                                       /// 

                                       runningActors.Add(new RunningActors(address));
                                   }

                                   Console.WriteLine("We found a running actor so er did nothing");
                               }
                               else
                               {
                                   //var customer = new Actor<Customer>(actor.Context, new BinarySerializer());
                                   // customer.StartWithIdAndMethod(address, methodInfo, parameters);
                                   Console.WriteLine("no collection of running actors, So I am creating one and starting a new runner");
                                   ////start actor
                                   /// 
                                   runningActors = new List<RunningActors>();
                                   runningActors.Add(new RunningActors(address));
                                   actor.PropertyBag.Add("RunningActors", runningActors);
                               }
                           });
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