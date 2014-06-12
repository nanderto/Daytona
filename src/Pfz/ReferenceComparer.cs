using System.Collections.Generic;

namespace Pfz
{
	/// <summary>
	/// Class used to compare two references.
	/// They must point to the same instance (not an equal instance) to be
	/// considered equal.
	/// </summary>
	public sealed class ReferenceComparer:
		IEqualityComparer<object>
	{
		/// <summary>
		/// Gets the ReferenceComparer single instance.
		/// </summary>
		public static readonly ReferenceComparer Instance = new ReferenceComparer();

		private ReferenceComparer()
		{
		}

		bool IEqualityComparer<object>.Equals(object x, object y)
		{
			return x == y;
		}
		int IEqualityComparer<object>.GetHashCode(object obj)
		{
			if (obj == null)
				return -1;

			return obj.GetHashCode();
		}
	}
}
