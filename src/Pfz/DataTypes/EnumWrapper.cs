using System;
using Pfz.Extensions;

namespace Pfz.DataTypes
{
	/// <summary>
	/// Wrapper for enums that shows their DisplayName (which can be set by DisplayNameAttribute)
	/// when ToString is invoked.
	/// </summary>
	public struct EnumWrapper:
		IEquatable<EnumWrapper>
	{
		/// <summary>
		/// Creates a new wrapper over the given enum.
		/// </summary>
		public EnumWrapper(Enum value)
		{
			_value = value;
		}
		
		private Enum _value;
		/// <summary>
		/// Gets the original enum value.
		/// </summary>
		public Enum Value
		{
			get
			{
				return _value;
			}
		}

		/// <summary>
		/// Returns true if the passed object is another enum-wrapper for the same enum value.
		/// </summary>
		public override bool Equals(object obj)
		{
			if (!(obj is EnumWrapper))
				return false;
		
			EnumWrapper other = (EnumWrapper)obj;
			return Equals(other);
		}
		
		/// <summary>
		/// Returns true if the other enum has the same enum value than this.
		/// </summary>
		public bool Equals(EnumWrapper other)
		{
			return object.Equals(_value, other._value);
		}

		/// <summary>
		/// Gets the hashcode of the enum value or zero if it is null.
		/// </summary>
		public override int GetHashCode()
		{
			if (_value == null)
				return 0;
			
			return _value.GetHashCode();
		}
		
		/// <summary>
		/// Returns the display name of the enum value, or null if it is empty.
		/// </summary>
		public override string ToString()
		{
			if (_value == null)
				return null;
			
			return _value.GetDisplayName();
		}

		/// <summary>
		/// Compares two enum wrappers for equality.
		/// </summary>
		public static bool operator == (EnumWrapper a, EnumWrapper b)
		{
			return a.Equals(b);
		}

		/// <summary>
		/// Compares two enum wrappers for inequality.
		/// </summary>
		public static bool operator != (EnumWrapper a, EnumWrapper b)
		{
			return !a.Equals(b);
		}
	}
}
