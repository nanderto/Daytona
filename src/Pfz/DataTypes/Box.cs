using System;

namespace Pfz.DataTypes
{
	/// <summary>
	/// A class that simple "box" any value type.
	/// Sometimes I use this when I need to change single instances of integers and longs.
	/// Also, this is useful to simulate any "ref" parameter, if you can't really count
	/// with a ref parameter.
	/// </summary>
	/// <typeparam name="T">The type of the value to "box" inside a class.</typeparam>
	public class Box<T>:
		IEquatable<Box<T>>,
		IValueContainer<T>
	{
		/// <summary>
		/// Default constructor.
		/// </summary>
		public Box()
		{
		}
		
		/// <summary>
		/// Initializes the box with the given value.
		/// </summary>
		/// <param name="value">The value to initialize the box.</param>
		public Box(T value)
		{
			Value = value;
		}
	
		/// <summary>
		/// The boxed value. Used as a direct field so you can pass it when a ref is needed.
		/// </summary>
		public T Value;

		/// <summary>
		/// Compares this box with a value or another box.
		/// </summary>
		public override bool Equals(object obj)
		{
			if (obj is T)
				return object.Equals(Value, obj);
				
			Box<T> other = obj as Box<T>;
			if (other != null)
				return Equals(other);
			
			return base.Equals(obj);
		}
		
		/// <summary>
		/// Compares this box with another box.
		/// </summary>
		public bool Equals(Box<T> other)
		{
			if (other == null)
				return false;

			return object.Equals(Value, other.Value);
		}

		/// <summary>
		/// Gets the hashcode of the value or, if it is null, returns 0.
		/// </summary>
		public override int GetHashCode()
		{
			if (Value == null)
				return 0;
				
			return Value.GetHashCode();
		}

		/// <summary>
		/// Gets the ToString of the value held.
		/// </summary>
		public override string ToString()
		{
			if (Value == null)
				return null;
				
			return Value.ToString();
		}

		#region IValueContainer<T> Members
			T IReadOnlyValueContainer<T>.Value
			{
				get
				{
					return Value;
				}
			}
			void IWriteOnlyValueContainer<T>.SetValue(T value)
			{
				Value = value;
			}
		#endregion
		#region IValueContainer Members
			object IReadOnlyValueContainer.Value
			{
				get
				{
					return Value;
				}
			}
			void IWriteOnlyValueContainer.SetValue(object value)
			{
				Value = (T)value;
			}
		#endregion
	}
}
