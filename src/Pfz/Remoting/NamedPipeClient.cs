using System;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Threading;
using Pfz.Extensions;
using Pfz.Threading;

namespace Pfz.Remoting
{
	/// <summary>
	/// Static class capable of creating DuplexStreams that connects to a named
	/// pipe listener. Note that listeners are capable of accepting many clients,
	/// while the NamedPipeServerStream only accepts one client.
	/// </summary>
	public static class NamedPipeClient
	{
		/// <summary>
		/// Creates a duplex-stream connecting it to the given listener.
		/// </summary>
		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		public static DuplexStream<NamedPipeClientStream> Connect(string pipeName)
		{
			if (pipeName == null)
				throw new ArgumentNullException("pipeName");
			
			Mutex mutex = null;
			try
			{	
				AbortSafe.Run(() => mutex = new Mutex(false, "Pfz.Remoting.IpcClient:" + pipeName));

				mutex.WaitOne();
				try
				{
					string name = pipeName + "_Listener";
					
					NamedPipeClientStream client = null;
					try
					{
						AbortSafe.Run(() => client = new NamedPipeClientStream(".", name, PipeDirection.In, PipeOptions.WriteThrough));

						client.Connect(60000);
						
						var bytes = new byte[4];
						client.FullRead(bytes);
						int id = BitConverter.ToInt32(bytes, 0);
						
						return DuplexStream.CreateNamedPipeClient(".", pipeName + '_' + id, true);
					}
					finally
					{
						client.CheckedDispose();
					}
				}
				finally
				{
					mutex.ReleaseMutex();
				}
			}
			finally
			{
				mutex.CheckedDispose();
			}
		}
	}
}
