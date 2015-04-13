namespace Daytona
{
    using System;
    using System.Threading.Tasks;

    using Daytona.baseclasses;

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

        public static ActorReference Spawn(this Context context, string processName, string address, Action<object, Sender, Actor> action)
        {
            return Spawn(context, processName + @"/" + address, action);
        }

        public static ActorReference Spawn(this Context context, string processName, Action<object, Sender, Actor> action)
        {
            var task = Task.Run(() =>
                {
                    var serializer = context.MessageSerializerFactory.GetNewSerializer();
                    using (var actor = new Actor(context.NetMqContext, serializer, processName, processName, action))
                    {
                        actor.Start();
                    }
                });

            var messageSendingActor = new Actor(context.NetMqContext, context.MessageSerializerFactory);
            return new ActorReference { ActorReferenceTask = task, Address = processName, ActorFactory = messageSendingActor, MessageSerializerFactory = context.MessageSerializerFactory };
        }

        public static ActorReference Spawn(this Silo silo, string processName, string address, Action<object, Sender, Actor> action)
        {
            return Spawn(silo, processName + @"/" + address, action);
        }
    }
}