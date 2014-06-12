using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Pfz.DynamicObjects;

namespace Pfz.Remoting.Instructions
{
	[Serializable]
	internal sealed class InstructionSetProperty:
		Instruction
	{
		public long ObjectId;
		public PropertyInfo PropertyInfo;
		public object[] Indexes;
		public object Value;

		[SuppressMessage("Microsoft.Usage", "CA2219:DoNotRaiseExceptionsInExceptionClauses")]
		public override void Run(RemotingClient client, ThreadData threadData)
		{
			threadData._Action
			(
				false,
				PropertyInfo.GetSetMethod(),
				Indexes,
				() =>
				{
					var obj = client._objectsUsedByTheOtherSide.Get(ObjectId);

					InvokeProperty_EventArgs args = null;
					EventHandler<InvokeProperty_EventArgs> before = null;
					EventHandler<InvokeProperty_EventArgs> after = null;
					var events = client._parameters._invokePropertySetEvents;

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
							args.Value = Value;
						}
					}

					try
					{
						if (before != null)
						{
							before(this, args);
							if (!args.CanInvoke)
								return null;
						}

						PropertyInfo.SetValue(obj, Value, Indexes);
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
								after(this, args);

							var ex = args.Exception;
							if (ex != null)
								throw ex;
						}
					}

					return null;
				}
			);
		}
	}
}
