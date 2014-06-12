using System.Runtime.InteropServices;
using Pfz.DataTypes;
using System;

namespace Pfz.Caching
{
	/// <summary>
	/// Struct that holds both HandleType and Value of a Referece.
	/// This avoids doing two locks, for example.
	/// </summary>
	public struct ReferenceData<T>:
		IReferenceData<T>,
		IEquatable<ReferenceData<T>>
	{
		/// <summary>
		/// Creates a new ReferenceData with the given information.
		/// </summary>
		public ReferenceData(T value, GCHandleType handleType)
		{
			_handleType = handleType;
			_value = value;
		}

		private GCHandleType _handleType;
		/// <summary>
		/// Gets the HandleType.
		/// </summary>
		public GCHandleType HandleType
		{
			get
			{
				return _handleType;
			}
		}

		private T _value;
		/// <summary>
		/// Gets the Value.
		/// </summary>
		public T Value
		{
			get
			{
				return _value;
			}
		}

		/// <summary>
		/// Compares if this reference data equals another object.
		/// </summary>
		public override bool Equals(object obj)
		{
			if (obj is ReferenceData<T>)
			{
				var other = (ReferenceData<T>)obj;
				return Equals(other);
			}

			return false;
		}

		/// <summary>
		/// Compares if this reference data is idential to another one.
		/// </summary>
		public bool Equals(ReferenceData<T> other)
		{
			return _handleType == other._handleType && object.Equals(_value, other._value);
		}

		/// <summary>
		/// Gets the hashcode for this reference data.
		/// </summary>
		public override int GetHashCode()
		{
			var value = _value;
			if (_value == null)
				return _handleType.GetHashCode();
				
			return _handleType.GetHashCode() ^ _value.GetHashCode();
		}

		/// <summary>
		/// Compares two referece data for equality.
		/// </summary>
		public static bool operator == (ReferenceData<T> a, ReferenceData<T> b)
		{
			return a.Equals(b);
		}

		/// <summary>
		/// Compares two referece data for inequality.
		/// </summary>
		public static bool operator != (ReferenceData<T> a, ReferenceData<T> b)
		{
			return !a.Equals(b);
		}

		#region IValueContainer Members
			object IReadOnlyValueContainer.Value
			{
				get
				{
					return _value;
				}
			}
		#endregion
	}
}
