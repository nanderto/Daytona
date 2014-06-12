using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

namespace Pfz.Collections
{
	/// <summary>
	/// Struct that holds an immutable array.
	/// The methods that "change" the array will, in fact, create copies of it.
	/// </summary>
	public struct ImmutableArray<T>:
		IEquatable<ImmutableArray<T>>,
		IEnumerable<T>
	{
		private readonly T[] _array;
		
		/// <summary>
		/// Creates an immutable array that is a copy from the given collection.
		/// </summary>
		public ImmutableArray(ICollection<T> collection)
		{
			if (collection == null)
				throw new ArgumentNullException("collection");

			int count = collection.Count;
			if (count == 0)
			{
				_array = null;
				_hashCode = 0;
				return;
			}
			
			_array = new T[count];
			int index = -1;
			int hashCode = 0;
			foreach(T item in collection)
			{
				index++;
				
				_array[index] = item;
				if (item != null)
					hashCode ^= item.GetHashCode();
			}
			
			_hashCode = hashCode;
		}
		
		private ImmutableArray(T[] array, int hashCode)
		{
			_array = array;
			_hashCode = hashCode;
		}

		private readonly int _hashCode;
		/// <summary>
		/// Gets the hash-code of the array, which is the exclusive combination
		/// of all items hash-codes.
		/// </summary>
		public override int GetHashCode()
		{
			return _hashCode;
		}
		
		/// <summary>
		/// Compares this immutable array to another object.
		/// Returns false if the other object is not of the same type.
		/// </summary>
		public override bool Equals(object obj)
		{
			if (obj is ImmutableArray<T>)
			{
				var other = (ImmutableArray<T>)obj;
				return Equals(other);
			}
			
			return false;
		}

		/// <summary>
		/// Compares this immutable array to another one.
		/// </summary>
		public bool Equals(ImmutableArray<T> other)
		{
			if (other._hashCode != _hashCode)
				return false;
			
			if (other._array == _array)
				return true;
			
			if (_array == null || other._array == null)
				return false;
			
			if (other._array.Length != _array.Length)
				return false;
			
			return other._array.SequenceEqual(_array);
		}
		
		/// <summary>
		/// Creates a new immutable array, which all the items from this array and
		/// a new one at the end.
		/// </summary>
		public ImmutableArray<T> Add(T value)
		{
			T[] array;
			int hashCode;
			int valueHashCode = 0;
			if (value != null)
				valueHashCode = value.GetHashCode();
			
			if (_array == null)
			{
				array = new T[]{value};
				hashCode = valueHashCode;
			}
			else
			{
				int length = _array.Length;
				array = new T[length + 1];
				_array.CopyTo(array, 0);
				array[length] = value;
				hashCode = _hashCode ^ valueHashCode;
			}
			
			return new ImmutableArray<T>(array, hashCode);
		}
		
		/// <summary>
		/// Creates a copy of this immutable array, without the last item.
		/// Throws an IndexOutOfRangeException if this is an empty array.
		/// </summary>
		public ImmutableArray<T> RemoveLast()
		{
			if (_array == null)
				throw new ArgumentException("There are no more items to be removed.");
			
			int newLength = _array.Length - 1;
			
			if (newLength == 0)
				return new ImmutableArray<T>();
			
			T lastItem = _array[newLength];
			int lastHashCode = 0;
			if (lastItem != null)
				lastHashCode = lastItem.GetHashCode();
			
			T[] newArray = new T[newLength];
			Array.Copy(_array, newArray, newLength);
			int newHash = _hashCode ^ lastHashCode;
			return new ImmutableArray<T>(newArray, newHash);
		}
		
		/// <summary>
		/// Gets the length of this array.
		/// </summary>
		public int Length
		{
			get
			{
				if (_array == null)
					return 0;
				
				return _array.Length;
			}
		}
		
		/// <summary>
		/// Gets an item by the given index from this array.
		/// </summary>
		public T this[int index]
		{
			get
			{
				return _array[index];
			}
		}

		/// <summary>
		/// Returns the index of the first found value in this array.
		/// If the value does not exists, returns -1.
		/// </summary>
		public int IndexOf(T item)
		{
			if (_array == null)
				return -1;
			
			int count = _array.Length;
			for (int i=0; i<count; i++)
			{
				T otherItem = _array[i];
				
				if (object.Equals(otherItem, item))
					return i;
			}
			
			return -1;
		}

		/// <summary>
		/// Compares two ImmutableArrays for equality.
		/// </summary>
		public static bool operator == (ImmutableArray<T> a, ImmutableArray<T> b)
		{
			return a.Equals(b);
		}

		/// <summary>
		/// Compares two ImmutableArrays for inequality.
		/// </summary>
		public static bool operator != (ImmutableArray<T> a, ImmutableArray<T> b)
		{
			return !a.Equals(b);
		}

		#region IEnumerable<T> Members
			/// <summary>
			/// Gets an enumerator that will returns all items in this array.
			/// </summary>
			public IEnumerator<T> GetEnumerator()
			{
				if (_array == null)
					yield break;
					
				foreach(T value in _array)
					yield return value;
			}
		#endregion
		#region IEnumerable Members
			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		#endregion
	}
}
