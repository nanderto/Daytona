namespace Daytona
{
    using System;

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
}