namespace Daytona
{
    using System;
    using System.Threading.Tasks;
    
    public static class FrameworkExtensions
    {
        public static ActorReference Spawn(this Silo silo, string processName, Action<object, Sender, Actor> action)
        {
            var task = Task.Run(() =>
                {
                    var serializer = silo.MessageSerializerFactory.GetNewSerializer();
                    using (var actor = new Actor(silo.Context, serializer, processName, processName, action))
                    {
                        actor.Start();
                    }
                });

            return new ActorReference { ActorReferenceTask = task, Address = processName, ActorFactory = silo.ActorFactory, MessageSerializerFactory = silo.MessageSerializerFactory};
        }

        public static ActorReference Spawn(this Context context, string processName, string address, Action<object, Sender, AsyncSocket> action)
        {
            return Spawn(context, processName + @"/" + address, action);
        }

        public static ActorReference Spawn(this Context context, string processName, Action<object, Sender, AsyncSocket> action)
        {
            var task = Task.Run(async () =>
                {
                     var serializer = context.MessageSerializerFactory.GetNewSerializer();

                    using (var asyncSocket = new AsyncSocket(context, processName, processName, processName, serializer, action))
                    {
                        await asyncSocket.Start();
                    }
                });

            var messageSendingActor = new Actor(context.NetMqContext, context.MessageSerializerFactory);
            return new ActorReference { ActorReferenceTask = task, Address = processName, ActorFactory = messageSendingActor, MessageSerializerFactory = context.MessageSerializerFactory };
            ////MessageSerializerFactory = context.MessageSerializerFactory this should probably not reference the same factory but create a new one
        }

        public static ActorReference CreateActorReference(this Context context, string processName)
        {
            var messageSendingActor = new Actor(context.NetMqContext, context.MessageSerializerFactory);
            return new ActorReference { Address = processName, ActorFactory = messageSendingActor, MessageSerializerFactory = context.MessageSerializerFactory };
        }

        public static ActorReference CreateActorReference(this Silo silo, string processName)
        {
            var messageSendingActor = new Actor(silo.Context, silo.MessageSerializerFactory);
            return new ActorReference { Address = processName, ActorFactory = messageSendingActor, MessageSerializerFactory = silo.MessageSerializerFactory };
        }

        public static ActorReference Spawn(this Silo silo, string processName, string address, Action<object, Sender, Actor> action)
        {
            return Spawn(silo, processName + @"/" + address, action);
        }
    }
}