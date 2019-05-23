// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncSocket.cs" company="N.K.Anderton">
// Copyright © 2015 All Rights Reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Daytona
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;

    using NetMQ;
    using NetMQ.Sockets;
    using System.Text;

    public class AsyncSocket : IDisposable
    {
        private readonly Context Context;

        private readonly Action<object, Sender, AsyncSocket> Workload;

        public readonly string Name;

        private readonly string serviceAddress;

        private bool IsRunning;

        private NetMQSocket subscriberSocket;

        private TaskCompletionSource<bool> taskCompletionSource;

        public AsyncSocket(
            Context context,
            string address,
            string name,
            string inRoute,
            ISerializer serializer,
            Action<object, Sender, AsyncSocket> workload)
        {
            this.Serializer = serializer;
            this.IsRunning = false;
            this.Context = context;
            this.serviceAddress = address;
            this.Name = name;
            this.Workload = workload;
            this.PropertyBag = new Dictionary<string, object>();
            this.SetUpOutputChannel(context.NetMqContext);
            this.SetUpReceivers(context, inRoute);
            this.NetMQScheduler = new NetMQScheduler(context.NetMqContext, context.Poller);

            if (!context.Poller.IsStarted)
            {
                Task.Factory.StartNew(() => context.Poller.Start(), TaskCreationOptions.LongRunning);
            }
        }

        public Actor Actor { get; private set; }

        public string InRoute { get; set; }

        public NetMQScheduler NetMQScheduler { get; set; }

        public PublisherSocket OutputChannel { get; set; }

        public Dictionary<string, object> PropertyBag { get; set; }

        public ISerializer Serializer { get; set; }

        //public Func<object, CallBackEventArgs, object> SaveCompletedEvent { get; private set; }

       // public event EventHandler<CallBackEventArgs> SaveCompletedEvent;


        public event EventHandler<CallBackEventArgs> SaveCompletedEvent;

        public bool OutputChannelDisposed { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        //public Task<bool> ReceiveAsync()
        //{
        //    var task = new Task<Task<bool>>(
        //        () =>
        //            {
        //                var newRequest = new TaskCompletionSource<bool>();
        //                this.Requests.Add(this.RequestId, newRequest);
        //               // this.taskCompletionSource = new TaskCompletionSource<bool>();

        //                return newRequest.Task;
        //            });

        //    //this.NetMQScheduler = new NetMQScheduler(this.Context.NetMqContext, this.Context.Poller);
        //    // will start the task on the scheduler which is the same thread as the Poller thread
        //    task.Start(this.NetMQScheduler);
        //    return task.Result;
        //}

        /// <summary>
        /// Receives the message, and translates it into objects that are called to do the work of the method
        /// </summary>
        /// <param name="subscriber">The socket that the messages are being received on</param>
        /// <returns>Stop signal, if true it has received a message to stop processing and exit</returns>
        public virtual bool ReceiveMessage(NetMQSocket subscriber)
        {
            var stopSignal = false;
            var methodParameters = new List<object>();
            var serializer = new BinarySerializer();
            MethodInfo returnedMethodInfo = null;
            var returnedMessageType = String.Empty;
            var returnedAddress = String.Empty;
            var returnAddress = String.Empty; //need to send the return address in message package

            returnedAddress = Actor.GetString(subscriber, serializer);
            returnedMessageType = Actor.GetString(subscriber, serializer);

            //if (returnedMessageType == "MethodInfo")
            //{
            //    var returned = Actor.GetMethodInfo(subscriber, serializer);
            //    returnedMethodInfo = returned.Item1;
            //    var hasMore = returned.Item2;
            //    while (hasMore)
            //    {
            //        hasMore = Actor.AddParameter(subscriber, serializer, methodParameters);
            //    }

            //    var inputParameters = new object[5];
            //    inputParameters[0] = returnedAddress;
            //    inputParameters[1] = returnAddress;
            //    inputParameters[2] = returnedMethodInfo;
            //    inputParameters[3] = methodParameters;
            //    inputParameters[4] = this;

            //    Delegate action = null;
            //    this.Actions.TryGetValue("MethodInfo", out action);

            //    try
            //    {
            //        action.DynamicInvoke(inputParameters);
            //    }
            //    catch (Exception ex)
            //    {
            //        this.parentActor.SendException(ex, returnedAddress);
            //    }

            //}

            if (returnedMessageType == "Spawned")
            {
                var hasMore = true;
                while (hasMore)
                {
                    hasMore = Actor.AddParameter(subscriber, serializer, methodParameters);
                }

                var inputParameters = new object[3];
                inputParameters[0] = methodParameters[0];
                inputParameters[1] = new Sender(returnedAddress);
                inputParameters[2] = this;

                this.Workload.DynamicInvoke(inputParameters);
            }

            //if (returnedMessageType == "Workload")
            //{
            //    var inputParameters = new object[4];
            //    inputParameters[0] = returnedAddress;
            //    inputParameters[1] = returnedMethodInfo;
            //    inputParameters[2] = methodParameters;
            //    inputParameters[3] = this;

            //    this.parentActor.Workload.DynamicInvoke(inputParameters);
            //}

            if (returnedMessageType.ToLower() == "stop")
            {
                stopSignal = true;
            }

            //if (returnedMessageType.ToLower() == "shutdownallactors")
            //{
            //    var inputParameters = new object[2];
            //    inputParameters[0] = "shutdownallactors";
            //    inputParameters[1] = this;
            //    Delegate action = null;
            //    this.parentActor.Actions.TryGetValue("ShutDownAllActors", out action);
            //    action.DynamicInvoke(inputParameters);
            //    //this.Workload.DynamicInvoke(inputParameters);
            //}

            //if (returnedMessageType.ToLower() == "handleexceptions")
            //{
            //    var returned = Actor.GetException(subscriber, serializer);
            //    var returnedException = returned.Item1;
            //    string AddressThatThrewException = String.Empty;
            //    if (returned.Item2)
            //    {
            //        var result = Actor.GetExceptionDetails(subscriber, serializer);
            //        AddressThatThrewException = result.Item1.AddressThatThrewException;
            //    }

            //    Delegate action = null;
            //    if (this.parentActor.Actions.TryGetValue("HandleExceptions", out action))
            //    {

            //        var inputParameters = new object[5];
            //        inputParameters[0] = "HandleExceptions";
            //        inputParameters[1] = this;
            //        inputParameters[2] = returnedException;
            //        inputParameters[3] = "this is a message";
            //        inputParameters[4] = AddressThatThrewException;

            //        action.DynamicInvoke(inputParameters);
            //        ////I have to do something to handle exceptions like restart the actor.
            //        System.Diagnostics.Debug.Assert(true);
            //    }
            //}

            return stopSignal;
        }

        public Task<bool> ReceiveAsync()
        {
            var tcs = new TaskCompletionSource<bool>();

            this.SaveCompletedEvent += (s, e) =>
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

           // tcs.Task.Start(this.NetMQScheduler);
            //Task.Run(this.NetMQScheduler);
            //Task.Run(() =>
            //{
            //    var task = Task.Factory.StartNew(this.NetMQScheduler);
            //    this.NetMQSchedule;
            //});

            return tcs.Task;
        }

        public async Task Start()
        {
            var result = await this.ReceiveAsync();
            if (!result)
            {
                await this.Start();
            }
        }

        protected void SetUpOutputChannel(NetMQContext context)
        {
            this.OutputChannel = context.CreatePublisherSocket();
            this.OutputChannel.Connect(Pipe.PublishAddress);
        }

        protected void SetUpReceivers(Context context, string inRoute)
        {
            this.InRoute = inRoute;

            this.subscriberSocket = this.Context.NetMqContext.CreateSubscriberSocket();
            this.subscriberSocket.ReceiveReady += this.OnReceiveReady;
            this.subscriberSocket.Connect(Pipe.SubscribeAddress);

            if (String.IsNullOrEmpty(inRoute))
            {
                this.subscriberSocket.Subscribe(String.Empty);
            }
            else
            {
                this.subscriberSocket.Subscribe(this.Serializer.GetBuffer(inRoute));
            }

            context.Poller.AddSocket(this.subscriberSocket);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.subscriberSocket != null)
                {
                    this.Context.Poller.RemoveSocket(this.subscriberSocket);
                   
                    this.subscriberSocket.Dispose();
                }
            }

            //// There are no unmanaged resources to release, but
            //// if we add them, they need to be released here.
        }

        private void OnReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            try
            {
                var result = this.ReceiveMessage(this.subscriberSocket);
                this.CallBack(result, null);
            }
            catch(Exception exception)
            {
                this.CallBack(false, exception);
            }
            
            //this.taskCompletionSource.SetResult(this.ReceiveMessage(this.subscriberSocket));
            //this.taskCompletionSource = null;
        }

        public void CallBack(bool result, Exception exception)
        {
            var eventArgs = new CallBackEventArgs { Result = result, Error = exception };

            this.SaveCompletedEvent(this, eventArgs);
        }

        private Dictionary<int, TaskCompletionSource<bool>> Requests = new Dictionary<int, TaskCompletionSource<bool>>();

        private int RequestId = 0;
    }
}