using System;
using System.Collections.Generic;
using Pfz.Collections;
using Pfz.DynamicObjects;

namespace Pfz.Extensions
{
	/// <summary>
	/// Adds some methods to the Dictionary generic class.
	/// </summary>
	public static class PfzDictionaryExtensions
	{
		/// <summary>
		/// Gets a value by it's key or, if it doesn't exist, returns the default
		/// value for TValue.
		/// </summary>
		public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
		{
			if (dictionary == null)
				throw new ArgumentNullException("dictionary");

			TValue result;
			dictionary.TryGetValue(key, out result);
			return result;
		}
		
		/// <summary>
		/// Tries to get a value by it's key. If it doesn't exist, creates a new
		/// one, adds it to the dicionary and returns it.
		/// </summary>
		public static TValue GetOrCreateValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
		where
			TValue: new()
		{
			if (dictionary == null)
				throw new ArgumentNullException("dictionary");

			TValue result;
			
			if (!dictionary.TryGetValue(key, out result))
			{
				result = new TValue();
				dictionary.Add(key, result);
			}
			
			return result;
		}
		
		internal static readonly object _readOnlySecurityToken = new object();
		/// <summary>
		/// Gets a read-only wrapper over this dictionary.
		/// </summary>
		public static IReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this IDictionary<TKey, TValue> modifiableDictionary)
		{
			return StructuralCaster.Cast<IReadOnlyDictionary<TKey, TValue>>(modifiableDictionary, _readOnlySecurityToken);
		}
	}
}
