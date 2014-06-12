using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Pfz.DynamicObjects;
using Pfz.Remoting.Instructions;

namespace Pfz.Remoting
{
	internal sealed class RemotingProxyObject:
		RemotingProxy,
		IProxyObject
	{
		internal RemotingProxyObject(RemotingClient client, Wrapped wrapped)
		{
			RemotingClient = client;
			Id = wrapped.Id;
			RecreateAssembly = wrapped.RecreateAssembly;
			RecreateTypeName = wrapped.RecreateTypeName;
			RecreateData = wrapped.RecreateData;
			CachedValues = wrapped.CachedValues;
		}

		public object ImplementedObject { get; internal set; }
		public Dictionary<Type, Dictionary<string, object>> CachedValues;

		[SuppressMessage("Microsoft.Usage", "CA2219:DoNotRaiseExceptionsInExceptionClauses")]
		public object InvokeMethod(MethodInfo methodInfo, Type[] genericArguments, object[] parameters)
		{
			_ReconnectIfNeeded();

			InvokeMethod_EventArgs args = null;
			EventHandler<InvokeMethod_EventArgs> before = null;
			EventHandler<InvokeMethod_EventArgs> after = null;
			var events = RemotingClient._parameters._invokeMethodEvents;

			if (events != null)
			{
				before = events._beforeRedirect;
				after = events._afterRedirect;

				if (before != null || after != null)
				{
					args = new InvokeMethod_EventArgs();
					args.GenericArguments = genericArguments;
					args.MethodInfo = methodInfo;
					args.Parameters = parameters;
					args.Target = ImplementedObject;
				}
			}

			object result = null;
			try
			{
				if (before != null)
				{
					before(RemotingClient, args);
					if (!args.CanInvoke)
						return args.Result;
				}

				var instruction = new InstructionInvokeMethod();
				instruction.GenericArguments = genericArguments;
				instruction.MethodInfo = methodInfo;
				instruction.ObjectId = Id;
				instruction.Parameters = parameters;

				return RemotingClient._Invoke(_GetReconnectPath(), instruction, methodInfo, parameters);
			}
			catch(Exception exception)
			{
				if (after == null || !args.CanInvoke)
					throw;

				args.Exception = exception;
			}
			finally
			{
				if (args != null)
				{
					if (after != null && args.CanInvoke)
					{
						args.Result = result;
						after(RemotingClient, args);
						result = args.Result;
					}

					var ex = args.Exception;
					if (ex != null)
						throw ex;
				}
			}

			return result;
		}

		private List<Instruction> _GetReconnectPath()
		{
			var reconnectPath = ReconnectPath;

			if (reconnectPath == null)
				return null;

			return new List<Instruction>(ReconnectPath);
		}

		[SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0"), SuppressMessage("Microsoft.Usage", "CA2219:DoNotRaiseExceptionsInExceptionClauses")]
		public object InvokePropertyGet(PropertyInfo propertyInfo, object[] indexes)
		{
			_ReconnectIfNeeded();

			InvokeProperty_EventArgs args = null;
			EventHandler<InvokeProperty_EventArgs> before = null;
			EventHandler<InvokeProperty_EventArgs> after = null;
			var events = RemotingClient._parameters._invokePropertyGetEvents;

			if (events != null)
			{
				before = events._beforeRedirect;
				after = events._afterRedirect;

				if (before != null || after != null)
				{
					args = new InvokeProperty_EventArgs();
					args.Indexes = indexes;
					args.PropertyInfo = propertyInfo;
					args.Target = ImplementedObject;
				}
			}

			object result = null;
			try
			{
				if (before != null)
				{
					before(RemotingClient, args);
					if (!args.CanInvoke)
						return args.Value;
				}

				if (CacheRemotePropertyValuesAttribute.GetValueFor(propertyInfo))
				{
					lock(this)
					{
						var cachedValues = CachedValues;
						if (cachedValues != null)
						{
							Dictionary<string, object> innerDictionary;
							if (cachedValues.TryGetValue(propertyInfo.DeclaringType, out innerDictionary))
								innerDictionary.TryGetValue(propertyInfo.Name, out result);
						}
					}
				}
				else
				{
					var instruction = new InstructionGetProperty();
					instruction.Indexes = indexes;
					instruction.ObjectId = Id;
					instruction.PropertyInfo = propertyInfo;

					result = RemotingClient._Invoke(_GetReconnectPath(), instruction);
				}
			}
			catch(Exception exception)
			{
				if (after == null || !args.CanInvoke)
					throw;

				args.Exception = exception;
			}
			finally
			{
				if (args != null)
				{
					if (after != null && args.CanInvoke)
					{
						args.Value = result;
						after(RemotingClient, args);
						result = args.Value;
					}

					var ex = args.Exception;
					if (ex != null)
						throw ex;
				}
			}

			return result;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0"), SuppressMessage("Microsoft.Usage", "CA2219:DoNotRaiseExceptionsInExceptionClauses")]
		public void InvokePropertySet(PropertyInfo propertyInfo, object[] indexes, object value)
		{
			_ReconnectIfNeeded();

			InvokeProperty_EventArgs args = null;
			EventHandler<InvokeProperty_EventArgs> before = null;
			EventHandler<InvokeProperty_EventArgs> after = null;
			var events = RemotingClient._parameters._invokePropertySetEvents;

			if (events != null)
			{
				before = events._beforeRedirect;
				after = events._afterRedirect;

				if (before != null || after != null)
				{
					args = new InvokeProperty_EventArgs();
					args.Indexes = indexes;
					args.PropertyInfo = propertyInfo;
					args.Target = ImplementedObject;
					args.Value = value;
				}
			}

			try
			{
				if (before != null)
				{
					before(RemotingClient, args);
					if (!args.CanInvoke)
						return;
				}

				var instruction = new InstructionSetProperty();
				instruction.Indexes = indexes;
				instruction.ObjectId = Id;
				instruction.PropertyInfo = propertyInfo;
				instruction.Value = value;

				RemotingClient._Invoke(_GetReconnectPath(), instruction);

				if (CacheRemotePropertyValuesAttribute.GetValueFor(propertyInfo))
				{
					Type declaringType = propertyInfo.DeclaringType;
					lock(this)
					{
						var cachedValues = CachedValues;
						if (cachedValues == null)
						{
							cachedValues = new Dictionary<Type, Dictionary<string, object>>();
							CachedValues = cachedValues;
						}

						Dictionary<string, object> innerDictionary;
						if (!cachedValues.TryGetValue(declaringType, out innerDictionary))
						{
							innerDictionary = new Dictionary<string, object>();
							cachedValues.Add(declaringType, innerDictionary);
						}

						innerDictionary[propertyInfo.Name] = value;
					}
				}
			}
			catch(Exception exception)
			{
				if (after == null)
					throw;

				args.Exception = exception;
			}
			finally
			{
				if (args != null)
				{
					if (after != null && args.CanInvoke)
						after(RemotingClient, args);

					var ex = args.Exception;
					if (ex != null)
						throw ex;
				}
			}
		}

		[SuppressMessage("Microsoft.Usage", "CA2219:DoNotRaiseExceptionsInExceptionClauses")]
		public void InvokeEventAdd(EventInfo eventInfo, Delegate handler)
		{
			_ReconnectIfNeeded();

			InvokeEvent_EventArgs args = null;
			EventHandler<InvokeEvent_EventArgs> before = null;
			EventHandler<InvokeEvent_EventArgs> after = null;
			var events = RemotingClient._parameters._invokeEventAddEvents;

			if (events != null)
			{
				before = events._beforeRedirect;
				after = events._afterRedirect;

				if (before != null || after != null)
				{
					args = new InvokeEvent_EventArgs();
					args.EventInfo = eventInfo;
					args.Handler = handler;
					args.Target = ImplementedObject;
				}
			}

			try
			{
				if (before != null)
				{
					before(RemotingClient, args);
					if (!args.CanInvoke)
						return;
				}

				var instruction = new InstructionAddEvent();
				instruction.EventInfo = eventInfo;
				instruction.Handler = handler;
				instruction.ObjectId = Id;

				RemotingClient._Invoke(_GetReconnectPath(), instruction);
			}
			catch(Exception exception)
			{
				if (after == null || !args.CanInvoke)
					throw;

				args.Exception = exception;
			}
			finally
			{
				if (args != null)
				{
					if (after != null && args.CanInvoke)
						after(RemotingClient, args);

					var ex = args.Exception;
					if (ex != null)
						throw ex;
				}
			}
		}

		[SuppressMessage("Microsoft.Usage", "CA2219:DoNotRaiseExceptionsInExceptionClauses")]
		public void InvokeEventRemove(EventInfo eventInfo, Delegate handler)
		{
			_ReconnectIfNeeded();

			InvokeEvent_EventArgs args = null;
			EventHandler<InvokeEvent_EventArgs> before = null;
			EventHandler<InvokeEvent_EventArgs> after = null;
			var events = RemotingClient._parameters._invokeEventRemoveEvents;

			if (events != null)
			{
				before = events._beforeRedirect;
				after = events._afterRedirect;

				if (before != null || after != null)
				{
					args = new InvokeEvent_EventArgs();
					args.EventInfo = eventInfo;
					args.Handler = handler;
					args.Target = ImplementedObject;
				}
			}

			try
			{
				if (before != null)
				{
					before(RemotingClient, args);
					if (!args.CanInvoke)
						return;
				}

				var instruction = new InstructionRemoveEvent();
				instruction.EventInfo = eventInfo;
				instruction.Handler = handler;
				instruction.ObjectId = Id;

				RemotingClient._Invoke(_GetReconnectPath(), instruction);
			}
			catch(Exception exception)
			{
				if (after == null || !args.CanInvoke)
					throw;

				args.Exception = exception;
			}
			finally
			{
				if (args != null)
				{
					if (after != null && args.CanInvoke)
						after(RemotingClient, args);

					var ex = args.Exception;
					if (ex != null)
						throw ex;
				}
			}
		}

		public override ReferenceOrWrapped GetBackReference()
		{
			return new BackObjectReference { Id = this.Id };
		}
	}
}
