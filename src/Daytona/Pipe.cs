using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQ;
using ZeroMQ.Devices;

namespace Daytona
{
    public class Pipe : IDisposable
    {
        public ZmqContext Context { get; set; }
        private CancellationTokenSource cancellationTokenSource;
        public static ForwarderDevice forwarderDevice = null;
        public static QueueDevice queueDevce = null;
        private bool disposed;
        public static string PublishAddressClient = "tcp://localhost:5550";
        public static string PublishAddressServer = "tcp://*:5550";
        public static string SubscribeAddressServer =  "tcp://*:5553";//"inproc://back";
        public static string SubscribeAddressClient = "tcp://localhost:5553";

        public static string PubSubControlFrontAddress = "tcp://*:5551";
        public static string PubSubControlFrontAddressClient = "tcp://localhost:5551";
        public static string PubSubControlBackAddressServer = "tcp://*:5552";//"inproc://pubsubcontrol";
        public static string PubSubControlBackAddressClient = "tcp://localhost:5552";

        public Pipe()
        {

        }

        //public Pipe(ZmqContext context)
        //{
        //    this.Context = context;
        //    Setup();
        //}

        public void Start(ZmqContext context)
        {
            forwarderDevice = new ForwarderDevice(context, PublishAddressServer, SubscribeAddressServer, DeviceMode.Threaded);
            forwarderDevice.Start();
            while (!forwarderDevice.IsRunning)
            { }

            queueDevce = new QueueDevice(context, PubSubControlBackAddressServer, PubSubControlFrontAddress, DeviceMode.Threaded);
            queueDevce.Start();
            while (!queueDevce.IsRunning)
            { }

            this.Context = context;
            cancellationTokenSource = new CancellationTokenSource();

            //Task.Run(() =>
            //{
            //    Setup(this.cancellationTokenSource.Token); 
            //});
        }

        public void Exit()
        {
            CleanUpDevices();

            cancellationTokenSource.Cancel();
        }

        private void CleanUpDevices()
        {
            if (queueDevce != null)
            {
                if (queueDevce.IsRunning)
                {
                    queueDevce.Stop();
                    queueDevce.Close();
                }
                queueDevce.Dispose();
            }

            if (forwarderDevice != null)
            {
                if (forwarderDevice.IsRunning)
                {
                    forwarderDevice.Stop();
                    forwarderDevice.Close();
                }
                forwarderDevice.Dispose();
            }
        }

        //private void Setup(CancellationToken cancellationToken)
        //{
        //    using (ZmqSocket frontend = this.Context.CreateSocket(SocketType.SUB), backend = this.Context.CreateSocket(SocketType.PUB))
        //    {
        //        frontend.Bind("tcp://*:5556");
        //        frontend.SubscribeAll();

        //        //  This is our public endpoint for subscribers
        //        backend.Bind("tcp://*:5555"); // i use local to be able to run the example, this could be the public ip instead eg. tcp://10.1.1.0:8100

        //        //var device = new ZeroMQ.Devices.ForwarderDevice(this.Context, "tcp://*:5556", "tcp://*:5555", DeviceMode.Blocking);
        //        //device.Start();
        //        //  Shunt messages out to our own subscribers
        //        int i = 0;
        //        try
        //        {
        //            while (true)
        //            {
        //                bool hasMore = true;
        //                var zmqMessage = new ZmqMessage();
        //                while (hasMore)
        //                {
        //                    Frame frame = frontend.ReceiveFrame();
        //                    //string message = frontend.Receive(Encoding.Unicode);
        //                    //message = message + i.ToString();
        //                    //Console.WriteLine(message);

        //                    zmqMessage.Append(new Frame(frame.Buffer));
        //                    hasMore = frontend.ReceiveMore;
        //                }
        //                i++;
        //                Writeline(i.ToString());
        //                backend.SendMessage(zmqMessage);
        //                cancellationToken.ThrowIfCancellationRequested();
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Writeline("cancelled gracefully exit " + ex.Message);
        //        }
        //    }
        //}

        //void Setup()
        //{

        //    using (ZmqSocket frontend = this.Context.CreateSocket(SocketType.SUB), backend = this.Context.CreateSocket(SocketType.PUB))
        //    {
        //        frontend.Bind("tcp://*:5556");
        //        frontend.SubscribeAll();

        //        //  This is our public endpoint for subscribers
        //        backend.Bind("tcp://*:5555"); // i use local to be able to run the example, this could be the public ip instead eg. tcp://10.1.1.0:8100

        //        //var device = new ZeroMQ.Devices.ForwarderDevice(this.Context, "tcp://*:5556", "tcp://*:5555", DeviceMode.Blocking);
        //        //device.Start();
        //        //  Shunt messages out to our own subscribers
        //        int i = 0;
        //        while (true)
        //        {
        //            bool hasMore = true;
        //            var zmqMessage = new ZmqMessage();
        //            while (hasMore)
        //            {
        //                string message = frontend.Receive(Encoding.Unicode);
                        
        //                Writeline(i.ToString());
        //                zmqMessage.Append(new Frame(Encoding.Unicode.GetBytes(message)));
        //                hasMore = frontend.ReceiveMore;       
        //            }

        //            backend.SendMessage(zmqMessage);
        //        }
        //    }
        //}

        public static void Writeline(string line)
        {
            FileInfo fi = new FileInfo(@"c:\dev\Pipe.log");
            var stream = fi.AppendText();
            stream.WriteLine(line);
            stream.Flush();
            stream.Close();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    CleanUpDevices();
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }
            disposed = true;
        }
    }
}
