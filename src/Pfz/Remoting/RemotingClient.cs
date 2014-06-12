using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Pfz.Caching;
using Pfz.DataTypes;
using Pfz.DynamicObjects;
using Pfz.DynamicObjects.Internal;
using Pfz.Extensions;
using Pfz.Remoting.Instructions;
using Pfz.Serialization;

namespace Pfz.Remoting
{
	/// <summary>
	/// Class used to access objects remotelly.
	/// </summary>
	public class RemotingClient:
		RemotingCommon
	{
		#region Fields
			private StreamChanneller _channeller;

			internal BidirectionalDictionary _objectsUsedByTheOtherSide;
			internal WeakBidirectionalDictionary _wrappers = new WeakBidirectionalDictionary();

			internal readonly object _registeredEventsLock = new object();
			internal Dictionary<EventInfo, Dictionary<Delegate, WeakList<object>>> _registeredEvents;

			private ConditionalWeakTable<Thread, ThreadData> _threadDatas = new ConditionalWeakTable<Thread, ThreadData>();
		
			[Serializable]
			private sealed class FakeNull
			{
			}
			private static readonly FakeNull _fakeNull = new FakeNull();
		#endregion

		#region Constructors
			#region By ConnectionInfo
				/// <summary>
				/// Creates a new RemotingClient using the given connectionInfo.
				/// In this case, the RemotingClient is not able to reconnect.
				/// </summary>
				public RemotingClient(ConnectionInfo connectionInfo)
				{
					if (connectionInfo.Stream == null)
						throw new ArgumentException("connectionInfo.Stream can't be null.");

					_connectionInfo = connectionInfo;
					GCUtils.Collected += _Collected;
					_objectsUsedByTheOtherSide = new BidirectionalDictionary();
				}
			#endregion
			#region By ConnectOrReconnectFunction
				/// <summary>
				/// Creates a new RemotingClient using the given connectOrReconnectFunction.
				/// In this case, RemotingClient must allow reconnections, but you will still need to
				/// set CanReconnect to true.
				/// </summary>
				public RemotingClient(Func<ConnectionInfo> connectOrReconnectFunction)
				{
					if (connectOrReconnectFunction == null)
						throw new ArgumentNullException("connectOrReconnectFunction");

					_connectOrReconnectFunction = connectOrReconnectFunction;
					GCUtils.Collected += _Collected;
					_objectsUsedByTheOtherSide = new BidirectionalDictionary();
				}
			#endregion
			#region By Host and Port
				/// <summary>
				/// Creates a new RemotingClient, connecting to the given tcp/ip host and port.
				/// It can be made reconnectable by setting CanReconnect to true.
				/// </summary>
				public RemotingClient(string host, int port):
					this
					(
						() =>
						{
							var tcpClient = new TcpClient(host, port);
							tcpClient.NoDelay = true;

							return new ConnectionInfo(tcpClient, tcpClient.GetStream());
						}
					)
				{
				}
			#endregion

			#region Internal
				/// <summary>
				/// Constructor used to create a RemotingClient from the RemotingServer.CreateClient method.
				/// The parameters will be given by the server.
				/// </summary>
				internal protected RemotingClient(RemotingParameters parameters, ConnectionInfo connectionInfo):
					base(parameters)
				{
					if (connectionInfo.Stream == null)
						throw new ArgumentException("connectionInfo.Stream can't be null.");

					_connectionInfo = connectionInfo;
					GCUtils.Collected += _Collected;
					_objectsUsedByTheOtherSide = new BidirectionalDictionary();
				}
			#endregion
		#endregion
		#region Dispose
			/// <summary>
			/// Frees the unmanaged resources of this RemotingClient.
			/// </summary>
			protected override void Dispose(bool disposing)
			{
				if (disposing)
				{
					var channeller = _channeller;
					if (channeller != null)
					{
						_channeller = null;
						channeller.Dispose();
					}

					var stream = _connectionInfo.Stream;
					if (stream != null)
						stream.Dispose();

					var objectsUsedByTheOtherSide = _objectsUsedByTheOtherSide;
					if (objectsUsedByTheOtherSide != null)
					{
						_objectsUsedByTheOtherSide = null;
						objectsUsedByTheOtherSide.Dispose();
					}

					var wrappers = _wrappers;
					if (wrappers != null)
					{
						_wrappers = null;
						wrappers.Dispose();
					}

					var registeredEventsLock = _registeredEventsLock;
					if (registeredEventsLock != null)
					{
						lock(registeredEventsLock)
						{
							var registeredEvents = _registeredEvents;
							if (registeredEvents != null)
							{
								foreach(var pair in registeredEvents)
								{
									var eventInfo = pair.Key;
									var delegateDictionary = pair.Value;

									foreach(var pair2 in delegateDictionary)
									{
										var handler = pair2.Key;
										var weakList = pair2.Value;

										foreach(object obj in weakList)
											eventInfo.RemoveEventHandler(obj, handler);
									}
								}
							}
						}
					}

					GCUtils.Collected -= _Collected;

					var disposed = Disposed;
					if (disposed != null)
					{
						var args = new EventArgs<RemotingClient>();
						args.Value = this;
						disposed(_parameters._sender, args);
					}
				}

				base.Dispose(disposing);
			}
		#endregion
		#region _Collected
			private void _Collected()
			{
				try
				{
					lock(_registeredEventsLock)
					{
						var oldRegisteredEvents = _registeredEvents;

						if (oldRegisteredEvents != null)
						{
							var newRegisteredEvents = new Dictionary<EventInfo, Dictionary<Delegate, WeakList<object>>>();
							foreach(var pair in oldRegisteredEvents)
							{
								var delegateDictionary = pair.Value;
								var newDelegateDictionary = new Dictionary<Delegate, WeakList<object>>();

								foreach(var pair2 in delegateDictionary)
								{
									if (pair2.Value.Count > 0)
										newDelegateDictionary.Add(pair2.Key, pair2.Value);
								}

								if (newDelegateDictionary.Count > 0)
									newRegisteredEvents.Add(pair.Key, newDelegateDictionary);
							}

							if (newRegisteredEvents.Count > 0)
								_registeredEvents = newRegisteredEvents;
							else
								_registeredEvents = null;
						}
					}

					long[] collectedIds = _wrappers.Collect();
					if (collectedIds == null)
						return;

					var instruction = new InstructionObjectsCollected();
					instruction.ObjectIds = collectedIds;

					var threadData = _GetThreadData();
					threadData.Serialize(false, instruction);
				}
				catch
				{
					var channeller = _channeller;
					if (!WasDisposed && channeller != null && !channeller.WasDisposed)
						throw;
				}
			}
		#endregion

		#region Properties
			#region ExecutingClient
				[ThreadStatic]
				private static RemotingClient _executingClient;

				/// <summary>
				/// Gets the RemotingClient that invoked the actual method, directly or indirectly.
				/// Will return null if the actual method was not invoked by a remote call.
				/// </summary>
				public static RemotingClient ExecutingClient
				{
					get
					{
						return _executingClient;
					}
				}
			#endregion

			#region ConnectOrReconnectFunction
				private Func<ConnectionInfo> _connectOrReconnectFunction;
				/// <summary>
				/// Gets the function used to connect (or reconnect) to the remote side.
				/// </summary>
				public Func<ConnectionInfo> ConnectOrReconnectFunction
				{
					get
					{
						return _connectOrReconnectFunction;
					}
				}
			#endregion
			#region ConnectionInfo
				internal ConnectionInfo _connectionInfo;
				/// <summary>
				/// Gets the actual connectionInfo. This can change if the client reconnects.
				/// </summary>
				public ConnectionInfo ConnectionInfo
				{
					get
					{
						return _connectionInfo;
					}
				}
			#endregion
			#region CanReconnect
				private bool _canReconnect;
				/// <summary>
				/// Gets a value indicating if the remoting client will be able to reconnect.
				/// </summary>
				public bool CanReconnect
				{
					get
					{
						return _canReconnect;
					}
					set
					{
						CheckModifiable();

						if (value && _connectOrReconnectFunction == null)
							throw new RemotingException("CanReconnect can only be set to true if ConnectOrReconnectFunction is not null.");

						_canReconnect = value;
					}
				}
			#endregion

			#region IsStarted
				private bool _isStarted;
				/// <summary>
				/// Gets a value indicating if Start was already called or not.
				/// </summary>
				public bool IsStarted
				{
					get
					{
						return _isStarted;
					}
				}
			#endregion
		#endregion
		#region Methods
			#region GetFromRemoteObject
				/// <summary>
				/// Gets the RemotingClient that created the given remote object.
				/// Returns null if the object is not remote.
				/// </summary>
				public static RemotingClient GetFromRemoteObject(object obj)
				{
					BaseImplementedProxy baseImplementedProxy = obj as BaseImplementedProxy;
					if (baseImplementedProxy == null)
						return null;

					object fieldValue = baseImplementedProxy._proxyObject;
					RemotingProxy remotingProxy = fieldValue as RemotingProxy;
					if (remotingProxy == null)
						return null;

					return remotingProxy.RemotingClient;
				}
			#endregion
			#region TryReconnectIfNeeded
				/// <summary>
				/// Tries to reconnect a remote object.
				/// </summary>
				public static ReconnectResult TryReconnectIfNeeded(object remoteObject)
				{
					if (remoteObject == null)
						throw new ArgumentNullException("remoteObject");

					BaseImplementedProxy baseImplementedProxy = remoteObject as BaseImplementedProxy;
					if (baseImplementedProxy == null)
						throw new ArgumentException("The given object is not a valid remote object.");

					object fieldValue = baseImplementedProxy._proxyObject;
					RemotingProxy remotingProxy = fieldValue as RemotingProxy;
					if (remotingProxy == null)
						throw new ArgumentException("The given object is not a valid remote object.");

					return remotingProxy._TryReconnectIfNeeded();
				}
			#endregion
			#region ReconnectIfNeeded
				/// <summary>
				/// Reconnects a remote object, or throws an exception if it is not possible to reconnect.
				/// Returns true for sucessfull reconnection, and false when a reconnect is not needed.
				/// </summary>
				public static bool ReconnectIfNeeded(object remoteObject)
				{
					if (remoteObject == null)
						throw new ArgumentNullException("remoteObject");

					BaseImplementedProxy baseImplementedProxy = remoteObject as BaseImplementedProxy;
					if (baseImplementedProxy == null)
						throw new ArgumentException("The given object is not a valid remote object.");

					object fieldValue = baseImplementedProxy._proxyObject;
					RemotingProxy remotingProxy = fieldValue as RemotingProxy;
					if (remotingProxy == null)
						throw new ArgumentException("The given object is not a valid remote object.");

					return remotingProxy._ReconnectIfNeeded();
				}
			#endregion

			#region Start
				/// <summary>
				/// Starts this remoting client.
				/// Parameters will not accept changes anymore.
				/// </summary>
				public void Start()
				{
					CheckThread();

					if (_isStarted)
						throw new RemotingException("This RemotingClient is already running.");

					_parameters._isReadOnly = true;
					_isStarted = true;

					if (!_canReconnect)
						_Connect();
				}
			#endregion
			#region Disconnect
				/// <summary>
				/// Disconnects the active connection.
				/// This may end-up disposing the RemotingClient if it does not supports reconnections.
				/// </summary>
				public void Disconnect()
				{
					var channeller = _channeller;
					if (channeller != null)
						channeller.Dispose();
				}
			#endregion

			#region _Connect
				private bool _wasConnected;
				private void _Connect()
				{
					bool wasConnected = _wasConnected;
					try
					{
						if (wasConnected)
						{
							this._objectsUsedByTheOtherSide.Clear();
							this._registeredEvents = null;

							this._wrappers.ClearIds();

							this._threadDatas = new ConditionalWeakTable<Thread, ThreadData>();
						}

						Stream stream;
						var createStream = _connectOrReconnectFunction;
						if (createStream != null)
							_connectionInfo = createStream();

						stream = _connectionInfo.Stream;

						if (stream == null)
							throw new ArgumentException("connectionInfo.Stream must be, either set by setting ConnectionInfo manually or by setting ConnectOrReconnectFunction.");

						string hello = typeof(RemotingClient).Assembly.FullName;
						byte[] bytes = Encoding.UTF8.GetBytes(hello);
						byte[] bytesLength = BitConverter.GetBytes(bytes.Length);

						bool canTimeout = stream.CanTimeout;

						if (canTimeout)
							stream.WriteTimeout = 60000;

						stream.Write(bytesLength);
						stream.Write(bytes);
						stream.Flush();

						if (canTimeout)
							stream.ReadTimeout = 15000;

						stream.FullRead(bytesLength);

						if (BitConverter.ToInt32(bytesLength, 0) != bytes.Length)
							throw new RemotingException("Remote side is not a Pfz.Remoting connection.");

						stream.FullRead(bytes);
						if (Encoding.UTF8.GetString(bytes) != hello)
							throw new RemotingException("Remote side is not using the same version of Pfz.Remoting.");

						if (canTimeout)
						{
							stream.ReadTimeout = Timeout.Infinite;
							stream.WriteTimeout = Timeout.Infinite;
						}

						bool canThrow = _parameters._sender == this;
						_wasConnected = true;
						_channeller = new StreamChanneller(stream, _RunAsServer, BufferSizePerChannel, canThrow, _ChannellerDisposed);
					}
					catch(Exception exception)
					{
						if (!_canReconnect)
							Dispose(exception);

						throw;
					}

					var connected = Connected;
					if (connected != null)
					{
						var args = new ConnectedEventArgs(wasConnected);
						connected(this, args);
					}
				}
			#endregion
			#region _CreateConnectionIfNeeded
				private object _connectLock = new object();
				internal void _CreateConnectionIfNeeded()
				{
					CheckUndisposed();
			
					if (!_isStarted)
						throw new RemotingException("You must call Start().");

					if (!_canReconnect)
						return;

					var channeller = _channeller;
					if (channeller == null || channeller.WasDisposed)
					{
						lock(_connectLock)
						{
							channeller = _channeller;
							if (channeller == null || channeller.WasDisposed)
								_Connect();
						}
					}
				}
			#endregion
			#region _ChannellerDisposed
				private void _ChannellerDisposed(object sender, EventArgs args)
				{
					if (WasDisposed)
						return;

					if (!_canReconnect)
						Dispose();
				}
			#endregion
			#region _DisposeIfNeeded
				private void _DisposeIfNeeded(Exception exception)
				{
					if (_canReconnect)
						_channeller.Dispose();
					else
						Dispose(exception);
				}
			#endregion

			#region _GetWrappedDelegate
				internal object _GetWrappedDelegate(WrappedDelegate wrappedDelegate)
				{
					long id = wrappedDelegate.Id;

					RemotingProxyDelegate proxy = new RemotingProxyDelegate(this, id);
					_wrappers.Add(id, proxy);

					var result = DelegateProxier.Proxy(proxy, wrappedDelegate.DelegateType);
					proxy.ImplementedDelegate = result;
					return result;
				}
			#endregion
			#region _GetWrappedObject
				internal object _GetWrappedObject(Wrapped wrappedObject)
				{
					RemotingProxyObject proxy = new RemotingProxyObject(this, wrappedObject);

					_wrappers.Add(wrappedObject.Id, proxy);

					var result = InterfaceProxier.Proxy(proxy, wrappedObject.InterfaceTypes);
					proxy.ImplementedObject = result;
					return result;
				}
			#endregion
			#region _GetReferencedObject
				internal object _GetReferencedObject(RemotingSerializer serializer, Reference reference)
				{
					var result = _wrappers.Get(serializer, reference.Id);

					if (result == null)
						return null;

					RemotingProxyObject proxyObj = result as RemotingProxyObject;
					if (proxyObj != null)
						return proxyObj.ImplementedObject;

					RemotingProxyDelegate delegateProxy = (RemotingProxyDelegate)result;
					return delegateProxy.ImplementedDelegate;
				}
			#endregion
			#region _GetThreadData
				private ThreadData _GetThreadData()
				{
					Thread thread = Thread.CurrentThread;
					var threadData = _threadDatas.GetValue
					(
						thread,
						(ignored) => new ThreadData(_channeller.CreateChannel(), this)
					);

					return threadData;
				}
			#endregion
			#region _Invoke
				internal object _Invoke(List<Instruction> reconnectPath, Instruction instruction)
				{
					if (reconnectPath != null)
						reconnectPath.Add(instruction);

					var threadData = _GetThreadData();

					threadData.Serialize(false, instruction);

					object resultObject;
					while(true)
					{
						try
						{
							resultObject = threadData.Deserialize();
						}
						catch(Exception exception)
						{
							if (!WasDisposed)
								_DisposeIfNeeded(exception);

							throw;
						}

						instruction = resultObject as Instruction;
						if (instruction == null)
							break;

						instruction.Run(this, threadData);
					}

					RemotingResult result = (RemotingResult)resultObject;
					var exception2 = result.Exception;
					if (exception2 != null)
						throw exception2;

					var resultValue = result.Value;
					_SetReconnectPath(reconnectPath, threadData, resultValue);
					return resultValue;
				}
				internal object _Invoke(List<Instruction> reconnectPath, Instruction instruction, MethodInfo methodInfo, object[] outParameters)
				{
					if (reconnectPath != null)
						reconnectPath.Add(instruction);

					var threadData = _GetThreadData();

					threadData.Serialize(false, instruction);

					object resultObject;
					while(true)
					{
						try
						{
							resultObject = threadData.Deserialize();
						}
						catch(Exception exception)
						{
							if (!WasDisposed)
								_DisposeIfNeeded(exception);

							throw;
						}

						instruction = resultObject as Instruction;
						if (instruction == null)
							break;

						instruction.Run(this, threadData);
					}

					RemotingResult result = (RemotingResult)resultObject;
					var exception2 = result.Exception;
					if (exception2 != null)
						throw exception2;

					_ProcessOut(methodInfo, result.OutValues, outParameters);

					var resultValue = result.Value;
					_SetReconnectPath(reconnectPath, threadData, resultValue);
					return resultValue;
				}
			#endregion
			#region _SetReconnectPath
				private static void _SetReconnectPath(List<Instruction> reconnectPath, ThreadData threadData, object resultValue)
				{
					if (threadData.LastDeserializeAllowReconnect)
					{
						int expected = 0;

						var implemented = resultValue as BaseImplementedProxy;
						if (implemented != null)
						{
							var proxyObject = implemented._proxyObject as RemotingProxy;
							if (proxyObject != null)
							{
								if (reconnectPath != null)
									proxyObject.ReconnectPath = reconnectPath.ToArray();

								expected = 1;
							}
						}

						if (threadData.LastDeserializeWrapCount != expected)
							throw new RemotingException("Only a single reference should be generated for methods or properties that allow reconnections, and it must be the direct result.");
					}
				}
			#endregion


			#region _RunAsServer
				private void _RunAsServer(object sender, ChannelCreatedEventArgs args)
				{
					var data = args.Data;
					if (data != null)
					{
						if (data is FakeNull)
							args.Data = null;

						OnUserChannelCreated(_parameters._sender, args);

						return;
					}

					try
					{
						var channel = args.Channel;

						var thread = Thread.CurrentThread;
						var threadData = new ThreadData(channel, this);

						_threadDatas.Remove(thread);
						_threadDatas.Add(thread, threadData);

						while(true)
						{
							object instructionObject;

							try
							{
								instructionObject = threadData.Deserialize();
							}
							catch(Exception exception)
							{
								if (WasDisposed)
									return;

								_DisposeIfNeeded(exception);
								throw;
							}

							var instruction = (Instruction)instructionObject;

							var oldExecutingClient = _executingClient;
							try
							{
								_executingClient = this;
								instruction.Run(this, threadData);
							}
							finally
							{
								_executingClient = oldExecutingClient;
							}
						}
					}
					catch(Exception exception2)
					{
						if (!WasDisposed)
						{
							_DisposeIfNeeded(exception2);
							throw;
						}
					}
				}
			#endregion

			#region CreateUserChannel
				/// <summary>
				/// Creates an stream to communicate to the other side, without opening a new tcp ip port.
				/// </summary>
				public Channel CreateUserChannel(object serializableData = null)
				{
					_CreateConnectionIfNeeded();

					if (serializableData == null)
						serializableData = _fakeNull;

					var result = _channeller.CreateChannel(serializableData);
					return result;
				}
			#endregion

			#region InvokeStaticMethod
				/// <summary>
				/// Invokes a registered static method on the other side.
				/// </summary>
				public object InvokeStaticMethod(string methodName, params object[] parameters)
				{
					_CreateConnectionIfNeeded();
					var instruction = new InstructionInvokeStaticMethod();

					instruction.MethodName = methodName;
					instruction.Parameters = parameters;

					List<Instruction> reconnectPath = null;
					if (_canReconnect)
						reconnectPath = new List<Instruction>();

					return _Invoke(reconnectPath, instruction);
				}
			#endregion
			#region Create
				/// <summary>
				/// Creates a registered object on the other side.
				/// </summary>
				public object Create(string name, params object[] parameters)
				{
					_CreateConnectionIfNeeded();
					var instruction = new InstructionCreateObject();

					instruction.Name = name;
					instruction.Parameters = parameters;

					List<Instruction> reconnectPath = null;
					if (_canReconnect)
						reconnectPath = new List<Instruction>();

					return _Invoke(reconnectPath, instruction);
				}

				/// <summary>
				/// Creates an interface registered on the other side, using its default name and constructor.
				/// </summary>
				public T Create<T>()
				{
					return (T)Create(typeof(T).FullName);
				}
			#endregion

			#region Out Values in General
				private static readonly Dictionary<MethodInfo, int[]> _outIndexesDictionary = new Dictionary<MethodInfo, int[]>();
				private static readonly ReaderWriterLockSlim _outIndexesDictionaryLock = new ReaderWriterLockSlim();
				internal static int[] _GetOutIndexes(MethodInfo methodInfo)
				{
					bool result = false;
					int[] outIndexes = null;
					_outIndexesDictionaryLock.ReadLock
					(
						() => result = _outIndexesDictionary.TryGetValue(methodInfo, out outIndexes)
					);

					if (!result)
					{
						_outIndexesDictionaryLock.UpgradeableLock
						(
							() =>
							{
								result = _outIndexesDictionary.TryGetValue(methodInfo, out outIndexes);
								if (result)
									return;

								List<int> list = new List<int>();
								int parameterIndex = -1;
								foreach(var parameter in methodInfo.GetParameters())
								{
									parameterIndex++;

									if (parameter.ParameterType.IsByRef)
										list.Add(parameterIndex);
								}

								if (list.Count > 0)
									outIndexes = list.ToArray();

								_outIndexesDictionaryLock.WriteLock
								(
									() => _outIndexesDictionary.Add(methodInfo, outIndexes)
								);
							}
						);
					}

					return outIndexes;
				}
				private static void _ProcessOut(MethodInfo methodInfo, object[] resultOutParameters, object[] outParameters)
				{
					if (resultOutParameters == null)
						return;

					var outIndexes = _GetOutIndexes(methodInfo);
					int resultIndex = -1;
					foreach(int index in outIndexes)
					{
						resultIndex++;

						outParameters[index] = resultOutParameters[resultIndex];
					}
				}
				internal static object[] _GetOutValues(MethodInfo methodInfo, object[] outParameters)
				{
					if (outParameters == null)
						return null;

					var outIndexes = _GetOutIndexes(methodInfo);
					if (outIndexes == null)
						return null;

					int count = outIndexes.Length;
					object[] result = new object[count];
					for(int i=0; i<count; i++)
					{
						int index = outIndexes[i];
						result[i] = outParameters[index];
					}
					return result;
				}
			#endregion
		#endregion
		#region Events
			#region Connected
				/// <summary>
				/// Event invoked when the client connects or reconnects to the remote side.
				/// </summary>
				public event EventHandler<ConnectedEventArgs> Connected;
			#endregion
			#region Disposed
				/// <summary>
				/// Event invoked when this RemotingClient is disposed.
				/// To guarantee that it will be invoked, set this event in the RemotingClientParameters before
				/// creating it.
				/// </summary>
				public event EventHandler<EventArgs<RemotingClient>> Disposed;
			#endregion
		#endregion
	}
}
