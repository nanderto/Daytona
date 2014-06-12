using System;
using Pfz.Extensions;

namespace Pfz.DataTypes.Ranges
{
	/// <summary>
	/// Methods for discovering Range characteristics.
	/// </summary>
	public static class Range
	{
		/// <summary>
		/// Gets the RangeAttribute of the given rangeType.
		/// </summary>
		public static RangeAttribute GetRangeAttribute(Type rangeType)
		{
			if (rangeType == null)
				throw new ArgumentNullException("rangeType");

			var attribute = rangeType.GetCustomAttribute<RangeAttribute>();
			if (attribute == null)
				throw new ArgumentException(rangeType.FullName + " does not have a RangeAttribute associated with it.", "rangeType");
				
			return attribute;
		}

		/// <summary>
		/// Gets the typed minimum value of the given range.
		/// </summary>
		public static TValue GetMinimumValue<TRange, TValue>()
		where
			TRange: Range<TValue>
		where
			TValue: IComparable<TValue>
		{
			return (TValue)Convert.ChangeType(GetMinimumValue(typeof(TRange)), typeof(TValue));
		}
		
		/// <summary>
		/// Gets the untyped minimum value of the given range type.
		/// </summary>
		public static object GetMinimumValue(Type rangeType)
		{
			var attribute = GetRangeAttribute(rangeType);
				
			return attribute.MinimumValue;
		}
		
		/// <summary>
		/// Gets the typed maximum value of the given range.
		/// </summary>
		public static TValue GetMaximumValue<TRange, TValue>()
		where
			TRange: Range<TValue>
		where
			TValue: IComparable<TValue>
		{
			return (TValue)Convert.ChangeType(GetMaximumValue(typeof(TRange)), typeof(TValue));
		}
		
		/// <summary>
		/// Gets the untyped maximum value of the given range type.
		/// </summary>
		public static object GetMaximumValue(Type rangeType)
		{
			var attribute = GetRangeAttribute(rangeType);
				
			return attribute.MaximumValue;
		}
	}
	
	/// <summary>
	/// Base class responsible for holding values while validating its ranges.
	/// You must inherit it and apply a RangeAttribute to tell which values are 
	/// used as minimum and maximum.
	/// </summary>
	/// <typeparam name="T">The type of the Value property.</typeparam>
	[Serializable]
	public abstract class Range<T>:
		IRange,
		IValueContainer<T>
	where
		T: IComparable<T>
	{
		/// <summary>
		/// Instantiates a new Range with the given value.
		/// </summary>
		public Range(T value)
		{
			Value = value;
		}
	
		/// <summary>
		/// Gets the MinimumValue allowed by this range.
		/// </summary>
		public T MinimumValue
		{
			get
			{
				var minimumValue = Range.GetMinimumValue(GetType());
				return (T)Convert.ChangeType(minimumValue, typeof(T));
			}
		}
		
		/// <summary>
		/// Gets the MaximumValue allowed by this range.
		/// </summary>
		public T MaximumValue
		{
			get
			{
				var maximumValue = Range.GetMaximumValue(GetType());
				return (T)Convert.ChangeType(maximumValue, typeof(T));
			}
		}
		
		private T _value;
		/// <summary>
		/// Gets or sets the value.
		/// </summary>
		public T Value
		{
			get
			{
				return _value;
			}
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");
				
				if (value.CompareTo(MinimumValue) < 0)
				{
					string message = string.Format("value can't be less than {0}.", MinimumValue);
					throw new ArgumentOutOfRangeException("value", message);
				}
				
				if (value.CompareTo(MaximumValue) > 0)
				{
					string message = string.Format("value can't be greater than {0}.", MaximumValue);
					throw new ArgumentOutOfRangeException("value", message);
				}
				
				_value = value;
			}
		}

		#region IRange Members
			Type IRange.DataType
			{
				get 
				{
					return typeof(T);
				}
			}
			object IRange.MinimumValue
			{
				get 
				{
					return MinimumValue;
				}
			}

			object IRange.MaximumValue
			{
				get 
				{
					return MaximumValue;
				}
			}
		#endregion
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
				Value = (T)Convert.ChangeType(value, typeof(T));
			}
		#endregion
	}
}
