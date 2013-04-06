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

        public ZmqSocket MonitorChannel = null;

        private static ZmqSocket frontend, backend;

        private CancellationTokenSource cancellationTokenSource;

        private bool disposed;

        private Poller poller;

        public Pipe()
        {
        }

        public Pipe(ZmqContext context)
        {
            this.Context = context;
            this.Start(context);
        }

        public ZmqContext Context { get; set; }

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
            this.CleanUpDevices();
            this.cancellationTokenSource.Cancel();
        }

        public void Start(ZmqContext context)
        {
            this.SetUpMonitorChannel(context);

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

            this.Context = context;
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
                        poller = new Poller(new ZmqSocket[] { frontend, backend });

                        Writeline("About to start polling");

                        while (true)
                        {
                            poller.Poll();

                            Writeline("polling");
                            if (token.IsCancellationRequested)
                            {
                                break;
                            }
                        }
                    }
                }
            }, 
            token);
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

        private bool Writeline(string line)
        {
            this.MonitorChannel.Send(line, Encoding.Unicode);
            var signal = this.MonitorChannel.Receive(Encoding.Unicode, new TimeSpan(0, 0, 5));
            if (signal == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}