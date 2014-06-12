using System.Collections.Generic;
using System;

namespace Pfz.Collections
{
	/// <summary>
	/// This class creates an IFastEnumerator wrapper over a custom enumerator.
	/// Note that this class must be used in the server, so it passes the 
	/// IFastEnumerator to it's client. Using it locally or in the clients will
	/// probably make things slower.
	/// </summary>
	public sealed class FastEnumeratorWrapper<T>:
		IFastEnumerator<T>
	where
		T: class
	{
		private IEnumerator<T> _enumerator;
		
		/// <summary>
		/// Creates a new FastEnumerator over the given enumerable.
		/// </summary>
		public FastEnumeratorWrapper(IEnumerable<T> enumerable)
		{
			if (enumerable == null)
				throw new ArgumentNullException("enumerable");

			_enumerator = enumerable.GetEnumerator();
		}
		
		/// <summary>
		/// Creates a new FastEnumerator over the given enumerator.
		/// </summary>
		public FastEnumeratorWrapper(IEnumerator<T> enumerator)
		{
			_enumerator = enumerator;
		}
		
		/// <summary>
		/// Disposes the internal enumerator.
		/// </summary>
		public void Dispose()
		{
			var enumerator = _enumerator;
			if (enumerator != null)
			{
				_enumerator = null;
				enumerator.Dispose();
			}
		}

		#region IFastEnumerator<T> Members
			/// <summary>
			/// Gets the next value, or null.
			/// </summary>
			public T GetNext()
			{
				var enumerator = _enumerator;
				if (enumerator == null)
					return null;
				
				if (!enumerator.MoveNext())
					return null;
				
				return enumerator.Current;
			}
		#endregion
	}
}
