using System;
using System.Reflection;

namespace Pfz.DynamicObjects
{
	/// <summary>
	/// InvokeEvent - EventArgs. This arguments class is passed as parameter
	/// in before and after events of EventAdd and EventRemove.
	/// </summary>
	public sealed class InvokeEvent_EventArgs:
		Invoke_EventArgs
	{
		/// <summary>
		/// The EventInfo to which the handler must be added or removed.
		/// </summary>
		public EventInfo EventInfo { get; set; }
		
		/// <summary>
		/// The Handler to add or remove to the event.
		/// </summary>
		public Delegate Handler { get; set; }
	}
}
