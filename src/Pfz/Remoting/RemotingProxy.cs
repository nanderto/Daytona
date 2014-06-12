using Pfz.Remoting.Instructions;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace Pfz.Remoting
{
	
	internal abstract class RemotingProxy
	{
		internal RemotingProxy()
		{
		}

		public RemotingClient RemotingClient { get; internal set; }
		internal Instruction[] ReconnectPath { get; set; }
		internal long Id { get; set; }
		
		internal string RecreateAssembly { get; set; }
		internal string RecreateTypeName { get; set; }

		internal object RecreateData { get; set; }

		public abstract ReferenceOrWrapped GetBackReference();

		internal bool _ReconnectIfNeeded()
		{
			switch(_TryReconnectIfNeeded())
			{
				case ReconnectResult.None:
					return false;

				case ReconnectResult.NotReconnectable:
					throw new RemotingException("The connection was lost and the actual object is not reconnectable.");

				case ReconnectResult.ReconnectionFailed:
					throw new RemotingException("The object could not be reconnected.");

				case ReconnectResult.Reconnected:
					return true;
			}

			throw new RemotingException("Unknown ReconnectResult.");
		}

		internal ReconnectResult _TryReconnectIfNeeded()
		{
			RemotingClient._CreateConnectionIfNeeded();

			if (Id != -1)
				return ReconnectResult.None;

			var recreateData = RecreateData;
			var reconnectPath = ReconnectPath;
			if (recreateData == null && reconnectPath == null)
				return ReconnectResult.NotReconnectable;

			lock(this)
			{
				if (Id != -1)
					return ReconnectResult.None;

				var instruction = new InstructionRecreate();
				instruction.ReconnectPath = reconnectPath;
				instruction.RecreateAssembly = RecreateAssembly;
				instruction.RecreateTypeName = RecreateTypeName;
				instruction.RecreateData = recreateData;
				object objectId = RemotingClient._Invoke(null, instruction);
				long id = (long)objectId;
				if (id == -1)
					return ReconnectResult.ReconnectionFailed;

				_newWrapper = RemotingClient._wrappers.TryAdd(id, this);
				Id = id;
			}

			return ReconnectResult.Reconnected;
		}

		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification="Used to avoid another reference to be collected, destroying our destination object.")]
		internal object _newWrapper;
	}
}
