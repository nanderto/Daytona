using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Daytona
{
    using ZeroMQ;

    public class Silo : IDisposable
    {
        private ZmqContext context;
        private BinarySerializer binarySerializer;

        private bool disposed;

        public Silo(ZmqContext context, BinarySerializer binarySerializer)
        {
            this.context = context;
            this.binarySerializer = binarySerializer;
            ActorFactory = new Actor(context, new BinarySerializer(), string.Empty);
        }

        public Actor ActorFactory { get; set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
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
