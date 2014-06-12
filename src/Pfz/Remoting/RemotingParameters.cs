using System;
using System.Collections.Generic;
using System.Reflection;
using Pfz.DynamicObjects;

namespace Pfz.Remoting
{
	/// <summary>
	/// This class has the common parameters used by the RemotingClient and RemotingServer classes.
	/// </summary>
	public sealed class RemotingParameters
	{
		internal bool _isReadOnly;
		internal RemotingParameters()
		{
			_bufferSizePerChannel = 8 * 1024;

			_registeredStaticMethods = new Dictionary<string, MethodInfo>();
			_registeredTypes = new Dictionary<string, ConstructorInfo>();
		}

		internal object _sender;
		internal int _bufferSizePerChannel;
		internal Dictionary<string, ConstructorInfo> _registeredTypes;
		internal Dictionary<string, MethodInfo> _registeredStaticMethods;
		internal EventHandler<ChannelCreatedEventArgs> _userChannelCreated;
		internal RemotingEventGroup<InvokeMethod_EventArgs> _invokeMethodEvents;
		internal RemotingEventGroup<InvokeProperty_EventArgs> _invokePropertyGetEvents;
		internal RemotingEventGroup<InvokeProperty_EventArgs> _invokePropertySetEvents;
		internal RemotingEventGroup<InvokeEvent_EventArgs> _invokeEventAddEvents;
		internal RemotingEventGroup<InvokeEvent_EventArgs> _invokeEventRemoveEvents;
		internal RemotingEventGroup<InvokeDelegate_EventArgs> _invokeDelegateEvents;
	}
}
