using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Pfz.Collections;
using Pfz.DataTypes;
using Pfz.Threading;

namespace Pfz.Remoting
{
	/// <summary>
	/// Class that acts as a server to remoting connections.
	/// </summary>
	public class RemotingServer:
		RemotingCommon
	{
		#region Fields
			private readonly IListener _listener;
		#endregion

		#region Constructors
			/// <summary>
			/// Creates the server using the given parameters.
			/// </summary>
			public RemotingServer(IListener listener)
			{
				if (listener == null)
					throw new ArgumentNullException("listener");

				_listener = listener;
			}

			/// <summary>
			/// Creates a new RemotingServer object, that will listen at the given tcp/ip port.
			/// </summary>
			/// <param name="tcpIpPort"></param>
			public RemotingServer(int tcpIpPort)
			{
				var listener = new TcpListener(IPAddress.Any, tcpIpPort);
				_listener = new TcpListenerWrapper(listener, false);
			}
		#endregion
		#region Dispose
			/// <summary>
			/// Closes the server connection and all client connections.
			/// </summary>
			protected override void Dispose(bool disposing)
			{
				if (disposing)
				{
					var clients = _clients;
					if (clients != null)
					{
						_clients = null;
						foreach(var client in clients)
						{
							client.Disposed -= _ClientDisposed;
							client.Dispose();
						}
						clients.Dispose();
					}

					var listener = _listener;
					if (listener != null)
						listener.Dispose();
				}

				base.Dispose(disposing);
			}
		#endregion

		#region Methods
			#region _ClientDisposed
				private void _ClientDisposed(object sender, EventArgs<RemotingClient> args)
				{
					try
					{
						_clients.Remove(args.Value);
					}
					catch
					{
						if (!WasDisposed)
							throw;
					}
				}
			#endregion

			#region Start
				/// <summary>
				/// Starts the server and makes its parameters read-only.
				/// </summary>
				public void Start()
				{
					CheckThread();

					if (_parameters._isReadOnly)
						throw new RemotingException("The RemotingServer is already running.");

					_listener.Start();

					UnlimitedThreadPool.Run
					(
						() =>
						{
							try
							{
								while(!WasDisposed)
								{
									var connectionBox = new Box<object>();
									var stream = _listener.Accept(connectionBox);

									var gettingStream = GettingStream;
									if (gettingStream != null)
									{
										var args = new EventArgs<Stream>();
										args.Value = stream;
										gettingStream(this, args);
										stream = args.Value;
									}

									var connectionInfo = new ConnectionInfo(connectionBox.Value, stream);
									UnlimitedThreadPool.Run
									(
										() =>
										{
											RemotingClient client;

											try
											{
												client = CreateClient(_parameters, connectionInfo);
												client.Start();
											}
											catch
											{
												IDisposable disposable = connectionBox.Value as IDisposable;
												if (disposable != null)
													disposable.Dispose();

												return;
											}

											if (WasDisposed)
												return;

											_clients.Add(client);

											var clientConnected = ClientConnected;
											if (clientConnected != null)
											{
												var args = new EventArgs<RemotingClient>();
												args.Value = client;
												clientConnected(this, args);
											}
										}
									);
								}
							}
							catch
							{
								if (!WasDisposed)
									throw;
							}
						}
					);
				}
			#endregion
			#region CreateClient
				/// <summary>
				/// Executed when a client connects, so a "RemotingClient" object must be created.
				/// This method is here in case you need to return a more specific remoting client.
				/// 
				/// You must create the RemotingClient using the version that receives the the "parameters".
				/// </summary>
				protected virtual RemotingClient CreateClient(RemotingParameters parameters, ConnectionInfo connectionInfo)
				{
					var result = new RemotingClient(parameters, connectionInfo);
					return result;
				}
			#endregion

			#region GetConnectedClients
				private AutoTrimHashSet<RemotingClient> _clients = new AutoTrimHashSet<RemotingClient>();
				/// <summary>
				/// Gets an array with all connected clients.
				/// </summary>
				public RemotingClient[] GetConnectedClients()
				{
					return _clients.ToArray();
				}
			#endregion
		#endregion
		#region Events
			#region GettingStream
				/// <summary>
				/// Event invoked when a client-stream is got, but before using it.
				/// </summary>
				public event EventHandler<EventArgs<Stream>> GettingStream;
			#endregion
			#region ClientConnected
				/// <summary>
				/// Event invoked when just after a client connects.
				/// </summary>
				public event EventHandler<EventArgs<RemotingClient>> ClientConnected;
			#endregion
		#endregion
	}
}
