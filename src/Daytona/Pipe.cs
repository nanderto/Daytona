//-----------------------------------------------------------------------
// <copyright file="Pipe.cs" company="The Phantom Coder">
//     Copyright The Phantom Coder. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Daytona
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using ZeroMQ;
    using ZeroMQ.Devices;

    public class Pipe : IDisposable
    {
        public static ForwarderDevice ForwarderDevice = null;

        public static string MonitorAddressClient = "tcp://localhost:5560";  ////"inproc://pubsubcontrol";//

        public static string MonitorAddressServer = "tcp://*:5560";

        public static string PublishAddressClient = "tcp://localhost:5550";

        public static string PublishAddressServer = "tcp://*:5550";

        public static string PubSubControlBackAddressClient = "tcp://localhost:5552"; ////"inproc://pubsubcontrol";//

        public static string PubSubControlBackAddressServer = "tcp://*:5552"; ////"inproc://pubsubcontrol";//

        public static string PubSubControlFrontAddress = "tcp://*:5551";

        public static string PubSubControlFrontAddressClient = "tcp://localhost:5551";

        public static QueueDevice QueueDevce = null;

        public static string SubscribeAddressClient = "tcp://localhost:5553"; ////"inproc://back";

        public static string SubscribeAddressServer = "tcp://*:5553"; ////"inproc://back";

        public static string SubscriberCountAddress = "SubscriberCountAddress";

        public static Encoding ControlChannelEncoding = Encoding.Unicode;

        public ZmqSocket MonitorChannel = null;

        private ZmqSocket AddSubscriberCountChannel = null;

        private static ZmqSocket frontend, backend;

        private CancellationTokenSource cancellationTokenSource;

        private bool disposed;

        private Poller poller;

        public Pipe()
        {
        }

        public Pipe(ZmqContext context)
        {
            this.Start(context);
        }

        //public ZmqContext Context { get; set; }

        public static void WritelineToLogFile(string line)
        {
            FileInfo fi = new FileInfo(@"c:\dev\Pipe.log");
            var stream = fi.AppendText();
            stream.WriteLine(line);
            stream.Flush();
            stream.Close();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Exit()
        {
            ////DONT SHUT DOWN UNTILL THE SUBSCRIBERS HAVE ALL SHUT DOWN
            while (SubscriberCount > 0)
            {
                
            }
            this.cancellationTokenSource.Cancel();
            this.CleanUpDevices();
        }

        public void Start(ZmqContext context)
        {
            this.SetUpMonitorChannel(context);
            this.SetUpAddSubscriberCountChannel(context);

            ////this should work but the forwarder device appears to be broken - it does not use XSUb and XPUB sockets
            ////ForwarderDevice = new ForwarderDevice(context, PublishAddressServer, SubscribeAddressServer, DeviceMode.Threaded);
            ////ForwarderDevice.Start();
            ////while (!ForwarderDevice.IsRunning)
            ////{ }

            QueueDevce = new QueueDevice(context, PubSubControlBackAddressServer, PubSubControlFrontAddress, DeviceMode.Threaded);
            QueueDevce.Start();
            while (!QueueDevce.IsRunning)
            {
            }

            this.Writeline("Control channel started");

            long count = 0;
            this.cancellationTokenSource = new CancellationTokenSource();
            var token = this.cancellationTokenSource.Token;
            Task.Run(() =>
            {
                using (frontend = context.CreateSocket(SocketType.XSUB))
                {
                    using (backend = context.CreateSocket(SocketType.XPUB))
                    {
                        frontend.Bind(Pipe.PublishAddressServer); ////"tcp://*:5550");
                        backend.Bind(Pipe.SubscribeAddressServer); ////"tcp://*:5553");
                        frontend.ReceiveReady += new EventHandler<SocketEventArgs>(FrontendReceiveReady);
                        backend.ReceiveReady += new EventHandler<SocketEventArgs>(BackendReceiveReady);
                        this.AddSubscriberCountChannel.ReceiveReady += new EventHandler<SocketEventArgs>(AddSubscriberCountChannelReceiveReady);
                        using (poller = new Poller(new ZmqSocket[] { frontend, backend, this.AddSubscriberCountChannel }))
                        {
                            Writeline("About to start polling");

                            while (true)
                            {
                                poller.Poll(new TimeSpan(0,0,0,0,5));
                                Writeline("polling" + count);
                                count++;
                                if (token.IsCancellationRequested)
                                {
                                    Writeline("break");
                                    break;
                                }
                            }
                        }

                        Writeline("stopped polling and exiting");
                    }
                }
            },
            token);
        }

        private static void AddSubscriberCountChannelReceiveReady(object sender, SocketEventArgs e)
        {
            WritelineToLogFile("AddSubscriberCountChannelReceiveReady");
            var messageReceiver = new MessageReceiver();
            SubscriberCount = SubscriberCount + messageReceiver.ReceiveMessage((ZmqSocket)sender);
        }

        private static void BackendReceiveReady(object sender, SocketEventArgs e)
        {
            WritelineToLogFile("BackendReceiveReady");
            e.Socket.Forward(frontend);
        }

        private static void FrontendReceiveReady(object sender, SocketEventArgs e)
        {
            WritelineToLogFile("FrontendReceiveReady");
            e.Socket.Forward(backend);
        }

        private void CleanUpDevices()
        {
            if (QueueDevce != null)
            {
                if (QueueDevce.IsRunning)
                {
                    QueueDevce.Stop();
                    QueueDevce.Close();
                }

                QueueDevce.Dispose();
            }

            if (ForwarderDevice != null)
            {
                if (ForwarderDevice.IsRunning)
                {
                    ForwarderDevice.Stop();
                    ForwarderDevice.Close();
                }

                ForwarderDevice.Dispose();
            }

            if (this.MonitorChannel != null)
            {
                this.MonitorChannel.Dispose();
            }
            ////if (this.poller != null)
            ////{
            ////    this.poller.Dispose();
            ////}
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.CleanUpDevices();
                }

                //// There are no unmanaged resources to release, but
                //// if we add them, they need to be released here.
            }

            this.disposed = true;
        }

        private void SetUpMonitorChannel(ZmqContext context)
        {
            this.MonitorChannel = context.CreateSocket(SocketType.REQ);
            this.MonitorChannel.Connect(Pipe.MonitorAddressClient);
        }

        private void SetUpAddSubscriberCountChannel(ZmqContext zmqContext)
        {
            this.AddSubscriberCountChannel = zmqContext.CreateSocket(SocketType.SUB);
            this.AddSubscriberCountChannel.Connect(Pipe.SubscribeAddressClient);
            this.AddSubscriberCountChannel.Subscribe(Pipe.ControlChannelEncoding.GetBytes(Pipe.SubscriberCountAddress));
        }

        private bool Writeline(string line)
        {
            try
            {
                if (this.MonitorChannel != null)
                {
                    this.MonitorChannel.Send(line, Pipe.ControlChannelEncoding);
                    return ReadSignal();
                }
            }
            catch (Exception ex)
            {
                //Writeline(ex.ToString());
                if(ex.ToString()=="")
                {
                    return ReadSignal();
                }
            }

            return false;
        }
  
        private bool ReadSignal()
        {
            var signal = this.MonitorChannel.Receive(Pipe.ControlChannelEncoding, new TimeSpan(0, 0, 0, 0, 100));
            if (signal == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static int SubscriberCount { get; set; }
    }
}