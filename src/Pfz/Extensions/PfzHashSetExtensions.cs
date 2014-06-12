using System.Collections.Generic;
using Pfz.Collections;

namespace Pfz.Extensions
{
	/// <summary>
	/// Adds the AsReadOnly method.
	/// </summary>
	public static class PfzHashSetExtensions
	{
		/// <summary>
		/// Returns a read-only wrapper over this hashset.
		/// </summary>
		public static IReadOnlyHashSet<T> AsReadOnly<T>(this HashSet<T> hashset)
		{
			return hashset.StructuralCast<IReadOnlyHashSet<T>>(PfzDictionaryExtensions._readOnlySecurityToken);
		}
	}
}
