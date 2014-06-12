using System;
using Pfz.Threading.Contexts;

namespace Pfz.DataTypes
{
	/// <summary>
	/// Simple wrapper for string with validations for Emails.
	/// </summary>
	[Serializable]
	public struct EmailString:
		IEquatable<EmailString>
	{
		private string _value;
		
		/// <summary>
		/// Creates a new EmailString with the given value.
		/// Throws an exception if the e-mail is invalid.
		/// </summary>
		public EmailString(string value)
		{
			if (string.IsNullOrEmpty(value))
				_value = null;
			else
			{
				if (!Validate(value))
					ThreadErrors.AddError("\"" + value + "\" is not a valid e-mail.");
				
				_value = value;
			}
		}
		
		/// <summary>
		/// Validates if the given string is a valid e-mail.
		/// </summary>
		public static bool Validate(string value)
		{
			if (string.IsNullOrEmpty(value))
				return false;
				
			value = value.Trim();
			
			int indexAt = value.IndexOf('@');
			if (indexAt < 1)
				return false;
			
			if (value.IndexOf('.', indexAt) == -1)
				return false;
			
			if (value.IndexOf(';') != -1)
				return false;
			
			return true;
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
			if (obj is EmailString)
			{
				EmailString other = (EmailString)obj;
				return Equals(other);
			}
			
			return base.Equals(obj);
		}
		
		/// <summary>
		/// Compares this PasswordString with another PasswordString.
		/// </summary>
		public bool Equals(EmailString other)
		{
			return _value == other._value;
		}

		/// <summary>
		/// Compares the equality of two email strings.
		/// </summary>
		public static bool operator == (EmailString a, EmailString b)
		{
			return a._value == b._value;
		}

		/// <summary>
		/// Compares the inequality of two email strings.
		/// </summary>
		public static bool operator != (EmailString a, EmailString b)
		{
			return a._value != b._value;
		}
	}
}
