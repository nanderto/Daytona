﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Daytona.Store
{
    /// <summary>
    /// Defines the scope of access for a specified type of object. Provides both synchronos and asynchronos access to the operations.
    /// use async where possible
    /// </summary>
    /// <typeparam name="T"></typeparam>
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

        /// <summary>
        /// Save Async will send the save message, 
        /// ensure that the actor that is listening for the retun message is started and if not then starts it
        /// it returns a task that can be awaited  that will return the message received  
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
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
                var dbPayload = new DBPayload<T>();
                dbPayload.Payload = input;
                if (this.actor.OutputChannelDisposed == true)
                {
                    throw new ChannelDisposedException(new StringBuilder().Append("the OutputChannel on this actor has been Disposed, The Actor is : ").Append(this.actor.Id.ToString()).ToString());    
                }

                this.actor.SendOneMessageOfType<DBPayload<T>>(this.actor.OutRoute, dbPayload, this.actor.Serializer, this.actor.OutputChannel);
                
                ////Start the actor that is listening for return messages.
                if (this.actor.IsRunning == false)
                {
                    Task.Run(() =>
                    {
                        this.actor.Start<DBPayload<T>>();
                    });
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
    }
}

