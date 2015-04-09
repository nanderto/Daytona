namespace Daytona
{
    using System;
    using System.Threading.Tasks;

    [Serializable]
    public class ActorFactory
    {
        public ActorFactory()
        {
            
        }

        public ActorFactory(Actor factory)
        {
            this.Factory = factory;
        }
        
        [NonSerialized]
        private Actor factory;

        public virtual Actor Factory
        {
            get { return factory; }
            set { factory = value; }
        }       
    }

    public class ActorReference
    {
        public Task ActorReferenceTask { get; set; }

        public string Addres { get; set; }
    }

    public static class FrameworkExtensions
    {
        public static ActorReference Spawn(this Silo silo, string processName, Action<Actor> action)
        {
            var task = Task.Run(() =>
                {
                    var serializer = silo.MessageSerializerFactory.GetNewSerializer();
                    using (var actor = new Actor(silo.Context, serializer, processName, processName, action))
                    {
                        actor.Start();
                    }
                });

           return new ActorReference { ActorReferenceTask = task, Addres = processName };
        }

        public static ActorReference Spawn(this Silo silo, string processName, string address, Action<Actor> action)
        {
            return Spawn(silo, processName + @"/" + address, action);
        }
    }
}