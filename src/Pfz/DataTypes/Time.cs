using System;
using System.Globalization;

namespace Pfz.DataTypes
{
	/// <summary>
	/// A struct that represents Time only (instead of Date and Time).
	/// </summary>
	[Serializable]
	public struct Time:
		IEquatable<Time>,
		IComparable<Time>,
		IComparable
	{
		#region DefaultFormat
			private static string _defaultFormat = "HH:mm:ss";
			
			/// <summary>
			/// Gets or sets the default format used to display Time values.
			/// </summary>
			public static string DefaultFormat
			{
				get
				{
					return _defaultFormat;
				}
				set
				{
					_defaultFormat = value;
				}
			}
		#endregion
		#region Parse
			/// <summary>
			/// Parses the given text as a Time using the default format.
			/// </summary>
			public static Time Parse(string text)
			{
				DateTime result = DateTime.ParseExact(text, _defaultFormat, CultureInfo.InvariantCulture);
				return (Time)result;
			}
		#endregion
	
		#region Now
			/// <summary>
			/// Gets the actual time, without date information.
			/// </summary>
			public static Time Now
			{
				get
				{
					return new Time(DateTime.Now);
				}
			}
		#endregion
	
		#region Constructors
			/// <summary>
			/// Creates a new time value from a given integer time, which
			/// must be previously got from a Time.AsInteger.
			/// </summary>
			/// <param name="integerTime"></param>
			public Time(int integerTime)
			{
				_value = integerTime;
			}
			
			/// <summary>
			/// Creates a new time object from the specified parameters.
			/// </summary>
			public Time(int hour, int minute, int second, int millisecond):
				this(new DateTime(1, 1, 1, hour, minute, second, millisecond))
			{
			}
			
			/// <summary>
			/// Creates a new time object from the specified dateTime, obviouly
			/// getting only the time part.
			/// </summary>
			/// <param name="dateTime"></param>
			public Time(DateTime dateTime)
			{
				int value = dateTime.Hour;
				value *= 60;
				value += dateTime.Minute;
				value *= 60;
				value += dateTime.Second;
				value *= 1000;
				value += dateTime.Millisecond;
				
				_value = value;
			}
		#endregion
		
		#region Properties
			/// <summary>
			/// Gets the hour of this time object.
			/// </summary>
			public int Hour
			{
				get
				{
					return _value / (1000 * 60 * 60);
				}
			}
			
			/// <summary>
			/// Gets the minute of this time object.
			/// </summary>
			public int Minute
			{
				get
				{
					return (_value / (1000 * 60)) % 60;
				}
			}
			
			/// <summary>
			/// Gets the second of this time object.
			/// </summary>
			public int Second
			{
				get
				{
					return (_value / 1000) % 60;
				}
			}
			
			/// <summary>
			/// Gets the millisecond of this time object.
			/// </summary>
			public int Millisecond
			{
				get
				{
					return _value % 1000;
				}
			}
		
			private int _value;
			/// <summary>
			/// Gets this value as an integer representation.
			/// </summary>
			public int AsInteger
			{
				get
				{
					return _value;
				}
			}
		#endregion
		#region Methods
			#region Static comparisons
				/// <summary>
				/// Compares if two time objects are equal.
				/// </summary>
				public static bool operator == (Time time1, Time time2)
				{
					return time1._value == time2._value;
				}
				
				/// <summary>
				/// Compares if two time objects are different.
				/// </summary>
				public static bool operator != (Time time1, Time time2)
				{
					return time1._value != time2._value;
				}
				
				/// <summary>
				/// Compares if a time object is less than other.
				/// </summary>
				public static bool operator < (Time time1, Time time2)
				{
					return time1._value < time2._value;
				}
				
				/// <summary>
				/// Compares if a time object is greater than other.
				/// </summary>
				public static bool operator > (Time time1, Time time2)
				{
					return time1._value > time2._value;
				}
				
				/// <summary>
				/// Compares if a time object is less than or equal to
				/// another.
				/// </summary>
				public static bool operator <= (Time time1, Time time2)
				{
					return time1._value <= time2._value;
				}
				
				/// <summary>
				/// Compares if a time value is greater than or equal to
				/// another.
				/// </summary>
				public static bool operator >= (Time time1, Time time2)
				{
					return time1._value >= time2._value;
				}
			#endregion
			#region Implicit and explicit conversions
				/// <summary>
				/// Implicit convert a Time object to a DateTime object.
				/// </summary>
				/// <param name="time"></param>
				/// <returns></returns>
				public static implicit operator DateTime(Time time)
				{
					return new DateTime(1, 1, 1, time.Hour, time.Minute, time.Second, time.Millisecond);
				}
				
				/// <summary>
				/// Explicit converts a DateTime object to a Time object.
				/// </summary>
				/// <param name="dateTime"></param>
				/// <returns></returns>
				public static explicit operator Time(DateTime dateTime)
				{
					return new Time(dateTime);
				}
			#endregion

			/// <summary>
			/// Gets the hashcode of this time object.
			/// </summary>
			/// <returns></returns>
			public override int GetHashCode()
			{
				return _value.GetHashCode();
			}
			
			/// <summary>
			/// Returns true if this time object equals another object.
			/// </summary>
			public override bool Equals(object obj)
			{
				if (obj is Time)
				{
					Time other = (Time)obj;
					return Equals(other);
				}
				
				return base.Equals(obj);
			}
			
			/// <summary>
			/// Returns true if this time object equals the value of
			/// another time object.
			/// </summary>
			public bool Equals(Time other)
			{
				return _value == other._value;
			}

			/// <summary>
			/// Gets the time formatted.
			/// </summary>
			/// <returns></returns>
			public override string ToString()
			{
				DateTime dateTime = this;
				return dateTime.ToString(_defaultFormat);
			}
			
			/// <summary>
			/// Returns an integer value with the comparison result of 
			/// this time and another time. Negative value means this
			/// time is smaller than the other.
			/// </summary>
			public int CompareTo(Time other)
			{
				return _value - other._value;
			}

			int IComparable.CompareTo(object obj)
			{
				return CompareTo((Time)obj);
			}
		#endregion
	}
}
