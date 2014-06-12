using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Pfz.DynamicObjects;
using Pfz.Extensions;

namespace Pfz.Remoting.Instructions
{
	[Serializable]
	internal sealed class InstructionGetProperty:
		Instruction
	{
		public long ObjectId;
		public PropertyInfo PropertyInfo;
		public object[] Indexes;

		[SuppressMessage("Microsoft.Usage", "CA2219:DoNotRaiseExceptionsInExceptionClauses")]
		public override void Run(RemotingClient client, ThreadData threadData)
		{
			bool canReconnect = PropertyInfo.ContainsCustomAttribute<ReconnectableAttribute>();
			threadData._Action
			(
				canReconnect,
				PropertyInfo.GetGetMethod(),
				Indexes,
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

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2219:DoNotRaiseExceptionsInExceptionClauses")]
		private object _Run(RemotingClient client, object obj)
		{
			InvokeProperty_EventArgs args = null;
			EventHandler<InvokeProperty_EventArgs> before = null;
			EventHandler<InvokeProperty_EventArgs> after = null;
			var events = client._parameters._invokePropertyGetEvents;

			if (events != null)
			{
				before = events._beforeInvoke;
				after = events._afterInvoke;

				if (before != null || after != null)
				{
					args = new InvokeProperty_EventArgs();
					args.Indexes = Indexes;
					args.PropertyInfo = PropertyInfo;
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
						return args.Value;
				}

				result = PropertyInfo.GetValue(obj, Indexes);
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
						args.Value = result;
						after(this, args);
						result = args.Value;
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
