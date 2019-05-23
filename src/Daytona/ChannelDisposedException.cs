 namespace Daytona
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class ChannelDisposedException : Exception
    {
        public ChannelDisposedException()
        {

        }

        protected ChannelDisposedException(SerializationInfo info, StreamingContext context): base(info, context)
        {

        }

        public ChannelDisposedException(string message)
            : base(message)
        {

        }
    }
}