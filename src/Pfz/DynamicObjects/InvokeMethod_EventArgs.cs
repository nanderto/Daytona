using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Pfz.DynamicObjects
{
	/// <summary>
	/// InvokeMethod - EventArgs
	/// This arguments class is passed as parameter when a method is being 
	/// invoked.
	/// For generic method, the MethodInfo is a method already constructed
	/// from the type params.
	/// </summary>
	public class InvokeMethod_EventArgs:
		Invoke_EventArgs
	{
		/// <summary>
		/// The method info to invoke.
		/// </summary>
		public MethodInfo MethodInfo { get; set; }
		
		/// <summary>
		/// If the MethodInfo is a generic method, here are the GenericArguments (you can use this to call MakeGenericMethod).
		/// If the MethodInfo is not a generic method, this value must be null.
		/// </summary>
		[SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
		public Type[] GenericArguments { get; set; }

		/// <summary>
		/// The parameters used to call the method.
		/// </summary>
		[SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
		public object[] Parameters { get; set; }
		
		/// <summary>
		/// In Before event, you must set the Result value of the method
		/// if you set the CanInvoke to false. If you set the result value
		/// without setting CanInvoke to false your result value will be lost.
		/// In the After event, this will be the value returned from the
		/// real method invocation.
		/// </summary>
		public object Result { get; set; }
	}
}
