using System;
using System.Diagnostics.CodeAnalysis;

namespace Pfz.DynamicObjects
{
	/// <summary>
	/// InvokeProperty - EventArgs
	/// This arguments class is used as the parameter for Before and After
	/// invoking a property get or set.
	/// </summary>
	public sealed class InvokeDelegate_EventArgs:
		Invoke_EventArgs
	{
		/// <summary>
		/// The Handler to Invoke.
		/// </summary>
		public Delegate Handler { get; set; }
		
		/// <summary>
		/// The parameters.
		/// </summary>
		[SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
		public object[] Parameters { get; set; }

		/// <summary>
		/// Gets or sets the Result.
		/// </summary>
		public object Result { get; set; }
	}
}
