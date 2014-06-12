using System;
using System.Collections.Generic;
using Pfz.Collections;

namespace Pfz.Extensions
{
	/// <summary>
	/// Adds some methods to the IFastEnumerator, so you can do a foreach
	/// over it, convert it to a list or array.
	/// </summary>
	public static class PfzFastEnumeratorExtensions
	{
		/// <summary>
		/// Converts a normal enumerable to a fast-enumerator.
		/// </summary>
		public static IFastEnumerator<T> AsFastEnumerator<T>(this IEnumerable<T> enumerable)
		where
			T: class
		{
			return new FastEnumeratorWrapper<T>(enumerable);
		}

		/// <summary>
		/// Converts a fast enumerator to a custom IEnumerable (generic version).
		/// </summary>
		public static IEnumerable<T> AsEnumerable<T>(this IFastEnumerator<T> fastEnumerator)
		where
			T: class
		{
			if (fastEnumerator == null)
				throw new ArgumentNullException("fastEnumerator");
			
			while(true)
			{
				T result = fastEnumerator.GetNext();
				
				if (result == null)
					yield break;
				
				yield return result;
			}
		}

		/// <summary>
		/// Copies all items from this enumerator to a list.
		/// </summary>
		public static List<T> ToList<T>(this IFastEnumerator<T> fastEnumerator)
		where
			T: class
		{
			if (fastEnumerator == null)
				throw new ArgumentNullException("fastEnumerator");
			
			List<T> list = new List<T>();
			while(true)
			{
				T item = fastEnumerator.GetNext();
				
				if (item == null)
					return list;
				
				list.Add(item);
			}
		}

		/// <summary>
		/// Copies all items from this enumerator to an array.
		/// </summary>
		public static T[] ToArray<T>(this IFastEnumerator<T> fastEnumerator)
		where
			T: class
		{
			return ToList(fastEnumerator).ToArray();
		}
	}
}
