using System;
using System.Diagnostics.CodeAnalysis;
using Pfz.DynamicObjects;

namespace Pfz.Remoting.Instructions
{
	[Serializable]
	internal sealed class InstructionInvokeDelegate:
		Instruction
	{
		public long HandlerId;
		public object[] Parameters;

		[SuppressMessage("Microsoft.Usage", "CA2219:DoNotRaiseExceptionsInExceptionClauses")]
		public override void Run(RemotingClient client, ThreadData threadData)
		{
			threadData._Action
			(
				false,
				() =>
				{
					Delegate handler = (Delegate)client._objectsUsedByTheOtherSide.Get(HandlerId);

					var parameters = Parameters;

					InvokeDelegate_EventArgs args = null;
					EventHandler<InvokeDelegate_EventArgs> before = null;
					EventHandler<InvokeDelegate_EventArgs> after = null;

					var delegateEvents = client._parameters._invokeDelegateEvents;
					if (delegateEvents != null)
					{
						before = delegateEvents._beforeInvoke;
						after = delegateEvents._afterInvoke;

						if (before != null || after != null)
						{
							args = new InvokeDelegate_EventArgs();
							args.Handler = handler;
							args.Target = handler.Target;
							args.Parameters = parameters;
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

						result = handler.DynamicInvoke(parameters);
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
			);
		}
	}
}
