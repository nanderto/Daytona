using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Pfz.DynamicObjects;
using Pfz.Threading;

namespace Pfz.Remoting
{
	/// <summary>
	/// This class has the common parameters used by the RemotingClient and RemotingServer classes.
	/// </summary>
	public abstract class RemotingCommon:
		ThreadSafeExceptionAwareDisposable
	{
		#region Fields
			internal readonly RemotingParameters _parameters;
			internal readonly Thread _creatorThread;
		#endregion

		#region Constructors
			internal RemotingCommon()
			{
				_creatorThread = Thread.CurrentThread;
				_parameters = new RemotingParameters();
				_parameters._sender = this;
			}
			internal RemotingCommon(RemotingParameters parameters)
			{
				if (parameters == null)
					throw new ArgumentNullException("parameters");

				_creatorThread = Thread.CurrentThread;
				_parameters = parameters;
			}
		#endregion

		#region Properties
			#region BufferSizePerChannel
				/// <summary>
				/// Gets or sets the Size of the buffers used by each communication channel.
				/// </summary>
				public int BufferSizePerChannel
				{	
					get
					{
						return _parameters._bufferSizePerChannel;
					}
					set
					{
						CheckModifiable();

						if (value < 256)
							throw new ArgumentException("BufferSizePerChannel can't be less than 256.");

						_parameters._bufferSizePerChannel = value;
					}
				}
			#endregion
		#endregion
		#region Methods
			#region CheckThread
				/// <summary>
				/// Checks if the method is being called by the Thread that created this object.
				/// If not, throws a RemotingException.
				/// </summary>
				protected void CheckThread()
				{
					if (_creatorThread != Thread.CurrentThread)
						throw new RemotingException("Parameters can only be changed by the thread that created them.");
				}
			#endregion
			#region CheckModifiable
				/// <summary>
				/// Checks if the the parameters are still modifiable..
				/// If not, throws a RemotingException.
				/// </summary>
				protected void CheckModifiable()
				{
					CheckThread();

					if (_parameters._isReadOnly)
						throw new ReadOnlyException("Parameters can only be changed before calling Start().");
				} 
			#endregion

			#region RegisterType
				#region RegisterType<TInterface, TReal>
					/// <summary>
					/// Registers an interface by it's default name and the class that will implement it.
					/// </summary>
					[SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
					public void RegisterType<TInterface, TReal>()
					where
						TReal: TInterface, new()
					{
						CheckModifiable();

						if (!typeof(TInterface).IsInterface)
							throw new ArgumentException("TInterface must be an interface type.", "TInterface");

						if (!typeof(TInterface).IsPublic)
							throw new ArgumentException("Only public interfaces can be implemented.", "TInterface");

						if (!typeof(TInterface).IsAssignableFrom(typeof(TReal)))
							throw new ArgumentException("TReal must implement TInterface.");

						var constructor = typeof(TReal).GetConstructor(Type.EmptyTypes);
						if (constructor == null)
							throw new ArgumentException("Can't find the default constructor of " + typeof(TReal) + ".");

						_parameters._registeredTypes.Add
						(
							typeof(TInterface).FullName, 
							constructor
						);
					}
				#endregion

				#region ByConstructor
					/// <summary>
					/// Registers an object constructor and gives a name to it.
					/// </summary>
					public void RegisterType(string name, ConstructorInfo constructor)
					{
						if (name == null)
							throw new ArgumentNullException("name");

						if (constructor == null)
							throw new ArgumentNullException("constructor");

						_RegisterType(name, constructor);
					}
				#endregion
				#region By CreateExpression
					/// <summary>
					/// Registers an object constructor, found by an expression, and gives a name to it.
					/// </summary>
					public void RegisterType<T>(string name, Expression<Func<T>> createExpression)
					{
						if (name == null)
							throw new ArgumentNullException("name");

						if (createExpression == null)
							throw new ArgumentNullException("createExpression");

						_RegisterType(name, ReflectionHelper.GetConstructor(createExpression));
					}
				#endregion

				#region _RegisterType
					private void _RegisterType(string name, ConstructorInfo constructor)
					{
						CheckModifiable();

						foreach(var parameter in constructor.GetParameters())
							if (parameter.IsOut)
								throw new ArgumentException("constructor can't have ref or out parameters.", "constructor");

						_parameters._registeredTypes.Add(name, constructor);
					}
				#endregion
			#endregion
			#region RegisterStaticMethod
				/// <summary>
				/// Registers an static method and gives a name to find it.
				/// </summary>
				public void RegisterStaticMethod(string name, MethodInfo method)
				{
					if (name == null)
						throw new ArgumentNullException("name");

					if (method == null)
						throw new ArgumentNullException("method");

					if (!method.IsStatic)
						throw new ArgumentException("method must be static.", "method");

					_RegisterStaticMethod(name, method);
				}

				/// <summary>
				/// Registers an static method by a call expression and gives a name to it.
				/// </summary>
				public void RegisterStaticMethod<T>(string name, Expression<Func<T>> sampleCallExpression)
				{
					if (name == null)
						throw new ArgumentNullException("name");

					if (sampleCallExpression == null)
						throw new ArgumentNullException("sampleCallExpression");

					_RegisterStaticMethod(name, ReflectionHelper.GetMethod(sampleCallExpression));
				}

				/// <summary>
				/// Registers an static method by a call expression and gives a name to it.
				/// </summary>
				public void RegisterStaticMethod<T>(string name, Expression<Action> sampleCallExpression)
				{
					if (name == null)
						throw new ArgumentNullException("name");

					if (sampleCallExpression == null)
						throw new ArgumentNullException("sampleCallExpression");

					_RegisterStaticMethod(name, ReflectionHelper.GetMethod(sampleCallExpression));
				}

				/// <summary>
				/// Registers an static method by its own name.
				/// </summary>
				public void RegisterStaticMethod(MethodInfo method)
				{
					if (method == null)
						throw new ArgumentNullException("method");

					if (!method.IsStatic)
						throw new ArgumentException("method must be static.", "method");

					_RegisterStaticMethod(method.Name, method);
				}

				/// <summary>
				/// Registers an static method found by an expression and uses its own name.
				/// </summary>
				public void RegisterStaticMethod<T>(Expression<Func<T>> sampleCallExpression)
				{
					if (sampleCallExpression == null)
						throw new ArgumentNullException("sampleCallExpression");

					var method = ReflectionHelper.GetMethod(sampleCallExpression);
					_RegisterStaticMethod(method.Name, method);
				}

				/// <summary>
				/// Registers an static method found by an expression and uses its own name.
				/// </summary>
				public void RegisterStaticMethod(Expression<Action> sampleCallExpression)
				{
					if (sampleCallExpression == null)
						throw new ArgumentNullException("sampleCallExpression");

					var method = ReflectionHelper.GetMethod(sampleCallExpression);
					_RegisterStaticMethod(method.Name, method);
				}

				private void _RegisterStaticMethod(string name, MethodInfo method)
				{
					CheckModifiable();

					foreach(var parameter in method.GetParameters())
						if (parameter.IsOut)
							throw new ArgumentException("method can't have ref or out parameters.", "method");

					_parameters._registeredStaticMethods.Add(name, method);
				}
			#endregion
		#endregion

		#region Events
			internal void OnUserChannelCreated(object sender, ChannelCreatedEventArgs args)
			{
				EventHandler<ChannelCreatedEventArgs> handler;

				lock(_parameters)
					handler = _parameters._userChannelCreated;

				if (handler != null)
					handler(sender, args);
			}
			/// <summary>
			/// Event invoked when a call to CreateUserChannel is done in the remove side.
			/// </summary>
			public event EventHandler<ChannelCreatedEventArgs> UserChannelCreated
			{
				add
				{
					lock(_parameters)
						_parameters._userChannelCreated += value;
				}
				remove
				{
					lock(_parameters)
						_parameters._userChannelCreated -= value;
				}
			}

			/// <summary>
			/// Gets that can be raised before or after effectivelly invoking a local or remote method.
			/// </summary>
			public RemotingEventGroup<InvokeMethod_EventArgs> InvokeMethodEvents
			{
				get
				{
					CheckModifiable();

					var result = _parameters._invokeMethodEvents;
					if (result == null)
					{
						result = new RemotingEventGroup<InvokeMethod_EventArgs>();
						_parameters._invokeMethodEvents = result;
					}
					return result;
				}
			}

			/// <summary>
			/// Gets that can be raised before or after effectivelly invoking a local or remote property get.
			/// </summary>
			public RemotingEventGroup<InvokeProperty_EventArgs> InvokePropertyGetEvents
			{
				get
				{
					CheckModifiable();

					var result = _parameters._invokePropertyGetEvents;
					if (result == null)
					{
						result = new RemotingEventGroup<InvokeProperty_EventArgs>();
						_parameters._invokePropertyGetEvents = result;
					}
					return result;
				}
			}

			/// <summary>
			/// Gets that can be raised before or after effectivelly invoking a local or remote property set.
			/// </summary>
			public RemotingEventGroup<InvokeProperty_EventArgs> InvokePropertySetEvents
			{
				get
				{
					CheckModifiable();

					var result = _parameters._invokePropertySetEvents;
					if (result == null)
					{
						result = new RemotingEventGroup<InvokeProperty_EventArgs>();
						_parameters._invokePropertySetEvents = result;
					}
					return result;
				}
			}

			/// <summary>
			/// Gets that can be raised before or after effectivelly invoking a local or remote event add.
			/// </summary>
			public RemotingEventGroup<InvokeEvent_EventArgs> InvokeEventAddEvents
			{
				get
				{
					CheckModifiable();

					var result = _parameters._invokeEventAddEvents;
					if (result == null)
					{
						result = new RemotingEventGroup<InvokeEvent_EventArgs>();
						_parameters._invokeEventAddEvents = result;
					}
					return result;
				}
			}

			/// <summary>
			/// Gets that can be raised before or after effectivelly invoking a local or remote event remove.
			/// </summary>
			public RemotingEventGroup<InvokeEvent_EventArgs> InvokeEventRemoveEvents
			{
				get
				{
					CheckModifiable();

					var result = _parameters._invokeEventRemoveEvents;
					if (result == null)
					{
						result = new RemotingEventGroup<InvokeEvent_EventArgs>();
						_parameters._invokeEventRemoveEvents = result;
					}
					return result;
				}
			}

			/// <summary>
			/// Gets that can be raised before or after effectivelly invoking a local or remote delegate.
			/// </summary>
			public RemotingEventGroup<InvokeDelegate_EventArgs> InvokeDelegateEvents
			{
				get
				{
					CheckModifiable();

					var result = _parameters._invokeDelegateEvents;
					if (result == null)
					{
						result = new RemotingEventGroup<InvokeDelegate_EventArgs>();
						_parameters._invokeDelegateEvents = result;
					}
					return result;
				}
			}
		#endregion
	}
}
