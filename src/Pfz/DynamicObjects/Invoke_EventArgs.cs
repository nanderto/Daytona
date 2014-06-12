using System;

namespace Pfz.DynamicObjects
{
	/// <summary>
	/// Base class for filling Invoke arguments.
	/// This base class generates the try/catch invoke pattern, where
	/// the CanInvoke can be set in the before event to stop the original
	/// method and after method to be invoked, and the Exception property
	/// is used to call the After event as a catch clause.
	/// </summary>
	public class Invoke_EventArgs:
		EventArgs
	{
		private bool _canInvoke = true;
		
		/// <summary>
		/// Only used in Before events to know if the original method and the After event
		/// must be invoked.
		/// </summary>
		public bool CanInvoke
		{
			get
			{
				return _canInvoke;
			}
			set
			{
				_canInvoke = value;
			}
		}

		/// <summary>
		/// Can be different than null if the After event is called as a Catch
		/// clause. You can set it again to null if you threat the exception.
		/// </summary>
		public Exception Exception { get; set; }
		
		/// <summary>
		/// The wrapper object that is calling the event. This is different
		/// from sender, as the sender if the EventedWrapperGenerator.
		/// </summary>
		public object Target { get; set; }
	}
}
