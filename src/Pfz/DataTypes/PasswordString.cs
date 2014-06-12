using System;

namespace Pfz.DataTypes
{
	/// <summary>
	/// Wrapper struct, so factories can threat this type of string differently,
	/// doing encription or showing them in password boxes.
	/// </summary>
	[Serializable]
	public struct PasswordString:
		IEquatable<PasswordString>
	{
		private string _value;
		
		/// <summary>
		/// Creates a new PasswordString with the given value.
		/// </summary>
		public PasswordString(string value)
		{
			_value = value;
		}
		
		/// <summary>
		/// Gets the real-string.
		/// </summary>
		public override string ToString()
		{
			return _value;
		}
		
		/// <summary>
		/// Gets the hashcode of the real string.
		/// </summary>
		public override int GetHashCode()
		{
			if (_value == null)
				return 0;
				
			return _value.GetHashCode();
		}
		
		/// <summary>
		/// Compares this object to another object.
		/// </summary>
		public override bool Equals(object obj)
		{
			if (obj is PasswordString)
			{
				PasswordString other = (PasswordString)obj;
				return Equals(other);
			}
			
			return base.Equals(obj);
		}
		
		/// <summary>
		/// Compares this PasswordString with another PasswordString.
		/// </summary>
		public bool Equals(PasswordString other)
		{
			return _value == other._value;
		}

		/// <summary>
		/// Compares two password strings for equality.
		/// </summary>
		public static bool operator == (PasswordString a, PasswordString b)
		{
			return a._value == b._value;
		}

		/// <summary>
		/// Compares two password strings for inequality.
		/// </summary>
		public static bool operator != (PasswordString a, PasswordString b)
		{
			return a._value != b._value;
		}
	}
}
