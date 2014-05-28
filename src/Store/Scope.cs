using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Daytona.Store
{
    public class Scope<T> : IScope, IDisposable
    {
        private bool disposed;

        public Actor actor { get; set; }

        public Scope()
        {

        }

        public Scope(Actor actor)
        {
            this.actor = actor;
        }

        public int Save<T>(T input) where T : IPayload
        {
            Task<int> t = SaveAsync<T>(input);
            return t.Result;
            //var dbPayload = new DBPayload<T>();
            //dbPayload.Payload = input;
            //this.actor.Execute<T>(input);
            //this.actor.Start<DBPayload<T>>();
            //return 1;
        }

        public Task<int> SaveAsync<T>(T input) where T : IPayload
        {
            var tcs = new TaskCompletionSource<int>();

            this.actor.SaveCompletedEvent += (object s, CallBackEventArgs e) =>
            {
                if (e.Error != null)
                {
                    tcs.TrySetException(e.Error);
                }
                else if (e.Cancelled)
                {
                    tcs.TrySetCanceled();
                }
                else
                {
                    tcs.TrySetResult(e.Result);
                }
            };

            try
            {
                var dbPayload = new DBPayload<T> { Payload = input };

                if (this.actor.OutputChannelDisposed == true)
                {
                    throw new ChannelDisposedException(new StringBuilder().Append("the OutputChannel on this actor has been Disposed, The Actor is : ").Append(this.actor.Id.ToString()).ToString());    
                }

                this.actor.SendOneMessageOfType<DBPayload<T>>(this.actor.OutRoute, dbPayload, this.actor.Serializer, this.actor.OutputChannel);
                
                if (this.actor.IsRunning == false)
                {
                    Task.Run(() => this.actor.Start<DBPayload<T>>());
                }
            }

            catch (ChannelDisposedException e)
            {
                tcs.TrySetException(e);
            }

            return tcs.Task;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            disposed = false;
            if (!disposed)
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
            disposed = true;
        }


        public Task<TResult> GetAsync<TResult>(int id) where TResult : IPayload
        {
            var tcs = new TaskCompletionSource<TResult>();

            this.actor.SaveCompletedEvent += (object s, CallBackEventArgs e) =>
            {
                if (e.Error != null)
                {
                    tcs.TrySetException(e.Error);
                }
                else if (e.Cancelled)
                {
                    tcs.TrySetCanceled();
                }
                else
                {
                    tcs.TrySetResult((TResult)e.Payload);
                }
            };

            try
            {
                var dbPayload = new DBPayload<TResult>(){Id = id};

                if (this.actor.OutputChannelDisposed == true)
                {
                    throw new ChannelDisposedException(new StringBuilder().Append("the OutputChannel on this actor has been Disposed, The Actor is : ").Append(this.actor.Id.ToString()).ToString());
                }

                
                //var getter = Actor.NewInstance(new Getter());
                //var got = getter.Get(id);

                this.actor.SendOneMessageOfType<DBPayload<TResult>>(this.actor.OutRoute, dbPayload, this.actor.Serializer, this.actor.OutputChannel);

                if (this.actor.IsRunning == false)
                {
                    Task.Run(() => this.actor.Start<DBPayload<TResult>>());
                }
            }

            catch (ChannelDisposedException e)
            {
                tcs.TrySetException(e);
            }

            return tcs.Task;
        }


        public Task<List<T>> GetAllAsync<T>() where T : IPayload
        {
            throw new NotImplementedException(); 
        }
    }
}

