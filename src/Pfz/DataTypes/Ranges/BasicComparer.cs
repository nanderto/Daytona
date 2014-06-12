using System;
using Pfz.Extensions;

namespace Pfz.DataTypes.Ranges
{
	/// <summary>
	/// Class that has methods to discover information about BasicComparer generic
	/// sub-types.
	/// </summary>
	public static class BasicComparer
	{
		/// <summary>
		/// Gets the BasicComparerAttribute from a type or throws an exception.
		/// </summary>
		public static BasicComparerAttribute GetBasicComparerAttribute(Type comparerType)
		{
			if (comparerType == null)
				throw new ArgumentNullException("comparerType");
			
			var attribute = comparerType.GetCustomAttribute<BasicComparerAttribute>();
			if (attribute == null)
				throw new ArgumentException("Type " + comparerType.FullName + " does not have a BasicComparerAttribute.");
			
			return attribute;
		}
	}
	
	/// <summary>
	/// Base class to be sub-classed only to tell which validation must be done.
	/// Use it together with the BasicComparerAttribute.
	/// </summary>
	public abstract class BasicComparer<T>:
		IBasicComparer,
		IValueContainer<T>
	{
		/// <summary>
		/// Creates a new instance with the given value.
		/// </summary>
		public BasicComparer(T value)
		{
			Value = value;
		}
		
		private T _value;
		
		/// <summary>
		/// Gets or sets the value of this instance.
		/// </summary>
		public T Value
		{
			get
			{
				return _value;
			}
			set
			{
				T compareToValue = CompareToValue;
				IComparable comparable = (IComparable)value;
				int result = comparable.CompareTo(compareToValue);
				
				switch(Comparison)
				{
					case BasicComparison.GreaterThan:
						if (result <= 0)
							throw new ArgumentOutOfRangeException("The value must be greater than " + compareToValue);
						
						break;
							
					case BasicComparison.GreaterThanOrEqualTo:
						if (result < 0)
							throw new ArgumentOutOfRangeException("The value must be greater than or equal to " + compareToValue);
						
						break;

					case BasicComparison.LessThan:
						if (result >= 0)
							throw new ArgumentOutOfRangeException("The value must be less than " + compareToValue);
						
						break;
						
					case BasicComparison.LessThanOrEqualTo:
						if (result > 0)
							throw new ArgumentOutOfRangeException("The value must be less than or equal to " + compareToValue);
						
						break;

					default:
						throw new ArgumentException("Unknown Comparison value.");
				}
				
				_value = value;
			}
		}
		
		/// <summary>
		/// Gets the comparison that must be done.
		/// </summary>
		public BasicComparison Comparison
		{
			get
			{
				Type type = GetType();
				var attribute = BasicComparer.GetBasicComparerAttribute(type);
				return attribute.Comparison;
			}
		}
		
		/// <summary>
		/// Gets the value to compare to.
		/// </summary>
		public T CompareToValue
		{
			get
			{
				Type type = GetType();
				var attribute = BasicComparer.GetBasicComparerAttribute(type);
				return (T)Convert.ChangeType(attribute.CompareToValue, typeof(T));
			}
		}

		#region IValueContainer Members
			void IWriteOnlyValueContainer<T>.SetValue(T value)
			{
				Value = value;
			}
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
		#region IBasicComparer Members
			Type IBasicComparer.DataType
			{
				get
				{
					return typeof(T);
				}
			}
			object IBasicComparer.CompareToValue
			{
				get
				{
					return CompareToValue;
				}
			}
		#endregion
	}
}
