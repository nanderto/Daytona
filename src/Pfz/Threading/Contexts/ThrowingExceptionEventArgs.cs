using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Pfz.Threading.Contexts
{
	/// <summary>
	/// Arguments used by ThreadErrors.ThrowingException
	/// </summary>
	[Serializable]
	public sealed class ThrowingExceptionEventArgs:
		EventArgs
	{
		/// <summary>
		/// Gets a hashset with all the errors that are not bound to an specific data source.
		/// </summary>
		[SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
		public HashSet<object> GlobalErrors { get; set; }

		/// <summary>
		/// Gets a dictionary with all data-sources and its errors.
		/// </summary>
		[SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
		public Dictionary<object, HashSet<object>> ErrorsDictionary { get; set; }

		/// <summary>
		/// Gets or sets a value indicating that the event was handled properly.
		/// This can avoid the exception to be thrown.
		/// </summary>
		public bool WasHandled { get; set; }
	}
}
