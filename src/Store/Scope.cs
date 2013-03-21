using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Daytona.Store
{
    public delegate int CallBackWithID(IPayload payLoad);
    public delegate int AsyncMethodCaller<T>(T input);
    //public delegate SaveCompletedEventhandler();

    public class Scope<T> : IScope, IDisposable
    {
        private bool disposed;
        private Actor actor;

        public Scope()
        {
           
        }

        public Scope(Actor actor)
        {
            this.actor = actor;
            SaveCompletedEvent += this.actor.SaveCompletedEvent;
        }

        public int Save<T>(T input)
        {
            var dbPayload = new DBPayload<T>();
            dbPayload.Payload = input;
            this.actor.Execute<T>(input);
            this.actor.Start<DBPayload<T>>();
            return 1;
        }

        //public async Task<int> SaveAsync<T>(T input) where T : IPayload
        //{
        //    return await Task<int>.Factory.FromAsync(SaveAsynchonosly<T>(input), this.actor.CallBack);
        //}

        public Task<int> SaveAsync<T>(T input) where T : IPayload
        {
           
            var tcs = new TaskCompletionSource<int>();
            SaveCompletedEvent += (object s, SaveCompletedEventArgs e) =>
                {
                    if (e.Error != null) tcs.TrySetException(e.Error);
                    else if (e.Cancelled) tcs.TrySetCanceled();
                    else tcs.TrySetResult(e.Result);
                };


            return tcs.Task;
        }

        public event EventHandler SaveCompletedEvent;

        public void SaveCompletedEventHandler(object sender, SaveCompletedEventArgs e)
        {
            var tcs = new TaskCompletionSource<int>();
            if (e.Error != null) tcs.TrySetException(e.Error);
                    else if (e.Cancelled) tcs.TrySetCanceled();
                    else tcs.TrySetResult(e.Result);
        }

        public IAsyncResult SaveAsynchonosly<T>(T input) where T : IPayload
        {
            var dbPayload = new DBPayload<T>();
            dbPayload.Payload = input;
            this.actor.SendOneMessageOfType<DBPayload<T>>(this.actor.OutRoute, dbPayload, this.actor.Serializer, this.actor.OutputChannel);       
            this.actor.Start<DBPayload<T>>();
            return null;
        }
     
        public static void CallBack(IAsyncResult result)
        {
            string s = "";
        }

        //public IAsyncResult SaveAsynchonosly<T>(T input) where T : IPayload
        //{
        //    AsyncMethodCaller<T> caller = new AsyncMethodCaller<T>(this.Save<T>);
        //    //IAsyncResult result = caller.BeginInvoke(input, this.OnUpdateCompleted, null);
        //    IAsyncResult result = caller.BeginInvoke(input, new AsyncCallback(this.OnUpdateCompleted), null);
        //    return result;
        //}

        public int OnUpdateCompleted(IAsyncResult result)
        {
            AsyncResult aResult = (AsyncResult)result;
            AsyncMethodCaller<T> caller = (AsyncMethodCaller<T>)aResult.AsyncDelegate;
            //result.
           // return 1;
            return caller.EndInvoke(result);
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
    }
}
