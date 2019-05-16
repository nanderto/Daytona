namespace Daytona
{
    using System;
    using System.Threading.Tasks;

    [Serializable]
    public class ActorFactory : IDisposable
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

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.factory != null)
                {
                    this.factory.Dispose();
                }
            }
        }

    }

    public class ActorReference
    {
        public MessageSerializerFactory MessageSerializerFactory { get; set; }

        public Actor ActorFactory { get; set; }

        public Task ActorReferenceTask { get; set; }

        public string Address { get; set; }

        public void Tell(object message)
        {
            var serializer = MessageSerializerFactory.GetNewSerializer();
            this.ActorFactory.SendMessage(this.Address, message, serializer, this.ActorFactory.OutputChannel);
        }

        public void Kill()
        {
            var serializer = MessageSerializerFactory.GetNewSerializer();
            this.ActorFactory.SendKillSignal(serializer, this.ActorFactory.OutputChannel, this.Address);
            this.ActorFactory.Dispose();
        }
    }
}