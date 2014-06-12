using System;
using System.Diagnostics.CodeAnalysis;
using Pfz.DynamicObjects;
using Pfz.Remoting.Instructions;
using System.Collections.Generic;

namespace Pfz.Remoting
{
	internal sealed class RemotingProxyDelegate:
		RemotingProxy,
		IProxyDelegate
	{
		internal RemotingProxyDelegate(RemotingClient client, long handlerId)
		{
			RemotingClient = client;
			Id = handlerId;
		}

		public Delegate ImplementedDelegate;

		[SuppressMessage("Microsoft.Usage", "CA2219:DoNotRaiseExceptionsInExceptionClauses")]
		public object Invoke(object[] parameters)
		{
			_ReconnectIfNeeded();

			InvokeDelegate_EventArgs args = null;
			EventHandler<InvokeDelegate_EventArgs> before = null;
			EventHandler<InvokeDelegate_EventArgs> after = null;

			var delegateEvents = RemotingClient._parameters._invokeDelegateEvents;
			if (delegateEvents != null)
			{
				before = delegateEvents._beforeRedirect;
				after = delegateEvents._afterRedirect;

				if (before != null || after != null)
				{
					args = new InvokeDelegate_EventArgs();
					args.Handler = ImplementedDelegate;
					args.Target = this;
					args.Parameters = parameters;
				}
			}

			List<Instruction> reconnectPath = null;
			if (ReconnectPath != null)
				reconnectPath = new List<Instruction>(ReconnectPath);

			object result = null;
			try
			{
				if (before != null)
				{
					before(RemotingClient, args);

					if (!args.CanInvoke)
						return args.Result;
				}

				var instruction = new InstructionInvokeDelegate();
				instruction.HandlerId = Id;
				instruction.Parameters = parameters;

				try
				{
					return RemotingClient._Invoke(reconnectPath, instruction);
				}
				catch
				{
					if (!RemotingClient.WasDisposed)
						throw;

					return null;
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

		public override ReferenceOrWrapped GetBackReference()
		{
			return new BackDelegateReference() { Id = Id };
		}
	}
}
