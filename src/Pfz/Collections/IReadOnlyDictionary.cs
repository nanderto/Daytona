using System.Collections;
using System.Collections.Generic;
using Pfz.DynamicObjects;

namespace Pfz.Collections
{
	/// <summary>
	/// Interface used to get dictionaries as read-only.
	/// </summary>
	public interface IReadOnlyDictionary:
		IEnumerable
	{
		/// <summary>
		/// Gets the number of items in the dictionary.
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Gets all keys.
		/// </summary>
		ICollection Keys { get; }

		/// <summary>
		/// Gets all values.
		/// </summary>
		ICollection Values { get; }

		/// <summary>
		/// Gets an item by its key.
		/// </summary>
		object this[object key] { get; }

		/// <summary>
		/// Returns true if an item with the given key exists.
		/// </summary>
		bool ContainsKey(object key);

		/// <summary>
		/// Returns true if the given value exists in the dictionary.
		/// </summary>
		bool ContainsValue(object value);

		/// <summary>
		/// Tries to get a value by its key.
		/// Returns true if the a value with the given key was found, false otherwise.
		/// </summary>
		bool TryGetValue(object key, out object value);
	}

	/// <summary>
	/// Interface used to get dictionaries as read-only.
	/// </summary>
	public interface IReadOnlyDictionary<TKey, TValue>:
		IReadOnlyDictionary,
		IEnumerable<KeyValuePair<TKey, TValue>>
	{
		/// <summary>
		/// Gets all keys.
		/// </summary>
		new ICollection<TKey> Keys { get; }

		/// <summary>
		/// Gets all values.
		/// </summary>
		new ICollection<TValue> Values { get; }

		/// <summary>
		/// Gets an item by its key.
		/// </summary>
		TValue this[TKey key] { get; }

		/// <summary>
		/// Returns true if an item with the given key exists.
		/// </summary>
		bool ContainsKey(TKey key);

		/// <summary>
		/// Returns true if the given value exists in the dictionary.
		/// </summary>
		bool ContainsValue(TValue value);

		/// <summary>
		/// Tries to get a value for the given key.
		/// </summary>
		bool TryGetValue(TKey key, out TValue value);
	}
}
