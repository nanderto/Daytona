using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Daytona.Store
{
    public class Connection : IDisposable
    {
        private bool Disposed;
        private Dictionary<string, IScope> scopes = new Dictionary<string, IScope>();

        internal void AddScope<T>(Scope<T> scope)
        {
            this.scopes.Add(typeof(T).Name, (IScope)scope);
        }

        public async Task<int> Save<T>(T input) where T : IPayload
        {
            IScope scope = null;
            this.scopes.TryGetValue(typeof(T).Name, out scope);
            return await scope.SaveAsync<T>(input);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            Disposed = false;
            if (!Disposed)
            {
                if (disposing)
                {
                    //if (subscriber != null)
                    //{
                    //    subscriber.Dispose();
                    //}
                    //if (OutputChannel != null)
                    //{
                    //    OutputChannel.Dispose();
                    //}
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }
            Disposed = true;
        }


    }


}
