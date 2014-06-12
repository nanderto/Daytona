using System.Collections.Generic;

namespace Pfz.Collections
{
	/// <summary>
	/// Interface returned by AsReadOnly extension method to make a HashSet read-only.
	/// </summary>
	public interface IReadOnlyHashSet<T>:
		IEnumerable<T>
	{
		/// <summary>
		/// Gets the comparer used by this hashset.
		/// </summary>
		IEqualityComparer<T> Comparer { get; }

		/// <summary>
		/// Gets the number of items in this hashset.
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Verifies if a given item exists in the hashset.
		/// </summary>
		bool Contains(T item);

		/// <summary>
		/// Copies the elements of this hashset to an array.
		/// </summary>
		void CopyTo(T[] array);

		/// <summary>
		/// Copies the elements of this hashset to an array.
		/// </summary>
		void CopyTo(T[] array, int arrayIndex);

		/// <summary>
		/// Copies the elements of this hashset to an array.
		/// </summary>
		void CopyTo(T[] array, int arrayIndex, int count);

		/// <summary>
		/// Check HashSet for details.
		/// </summary>
		bool IsProperSubsetOf(IEnumerable<T> other);

		/// <summary>
		/// Check HashSet for details.
		/// </summary>
		bool IsProperSupersetOf(IEnumerable<T> other);

		/// <summary>
		/// Check HashSet for details.
		/// </summary>
		bool IsSubsetOf(IEnumerable<T> other);

		/// <summary>
		/// Check HashSet for details.
		/// </summary>
		bool IsSupersetOf(IEnumerable<T> other);

		/// <summary>
		/// Check HashSet for details.
		/// </summary>
		bool Overlaps(IEnumerable<T> other);

		/// <summary>
		/// Check HashSet for details.
		/// </summary>
		bool SetEquals(IEnumerable<T> other);
	}
}
