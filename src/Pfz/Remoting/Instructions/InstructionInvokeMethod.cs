using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Pfz.DynamicObjects;
using Pfz.Extensions;

namespace Pfz.Remoting.Instructions
{
	[Serializable]
	internal sealed class InstructionInvokeMethod:
		Instruction
	{
		public long ObjectId;
		public MethodInfo MethodInfo;
		public Type[] GenericArguments;
		public object[] Parameters;

		public override void Run(RemotingClient client, ThreadData threadData)
		{
			var methodInfo = MethodInfo;
			bool canReconnect = methodInfo.ContainsCustomAttribute<ReconnectableAttribute>();
			threadData._Action
			(
				canReconnect,
				methodInfo,
				Parameters,
				() =>
				{
					var obj = client._objectsUsedByTheOtherSide.Get(ObjectId);

					return _Run(client, obj);
				}
			);
		}
		public override object ReRun(RemotingClient client, object target)
		{
			return _Run(client, target);
		}

		[SuppressMessage("Microsoft.Usage", "CA2219:DoNotRaiseExceptionsInExceptionClauses")]
		private object _Run(RemotingClient client, object obj)
		{
			var methodInfo = MethodInfo;
			var genericArguments = GenericArguments;
			if (genericArguments != null)
				methodInfo = methodInfo.MakeGenericMethod(genericArguments);

			var parameters = Parameters;

			InvokeMethod_EventArgs args = null;
			EventHandler<InvokeMethod_EventArgs> before = null;
			EventHandler<InvokeMethod_EventArgs> after = null;
			var events = client._parameters._invokeMethodEvents;

			if (events != null)
			{
				before = events._beforeInvoke;
				after = events._afterInvoke;

				if (before != null || after != null)
				{
					args = new InvokeMethod_EventArgs();
					args.GenericArguments = genericArguments;
					args.MethodInfo = methodInfo;
					args.Parameters = parameters;
					args.Target = obj;
				}
			}

			object result = null;
			try
			{
				if (before != null)
				{
					before(this, args);
					if (!args.CanInvoke)
						return args.Result;
				}

				result = methodInfo.Invoke(obj, parameters);
			}
			catch (Exception exception)
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
						after(this, args);
						result = args.Result;
					}

					var ex = args.Exception;
					if (ex != null)
						throw ex;
				}
			}

			return result;
		}
	}
}
