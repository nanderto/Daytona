using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Pfz.DynamicObjects
{
	/// <summary>
	/// InvokeProperty - EventArgs
	/// This arguments class is used as the parameter for Before and After
	/// invoking a property get or set.
	/// </summary>
	public sealed class InvokeProperty_EventArgs:
		Invoke_EventArgs
	{
		/// <summary>
		/// The PropertyInfo of the property to invoke.
		/// </summary>
		public PropertyInfo PropertyInfo { get; set; }
		
		/// <summary>
		/// The indexes parameters for indexed properties or null.
		/// </summary>
		[SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
		public object[] Indexes { get; set; }
		
		/// <summary>
		/// For property gets, this property will refer to the result value.
		/// For property sets, this property will refer to the value passed
		/// as parameter to the set.
		/// </summary>
		public object Value { get; set; }
	}
}
