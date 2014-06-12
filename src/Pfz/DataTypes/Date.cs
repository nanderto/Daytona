using System;
using System.Globalization;

namespace Pfz.DataTypes
{
	/// <summary>
	/// Class that represents a Date without Time.
	/// </summary>
	[Serializable]
	public struct Date:
		IEquatable<Date>,
		IComparable<Date>,
		IComparable
	{
		#region DefaultFormat
			private static string _defaultFormat = "yyyy/MM/dd";
			
			/// <summary>
			/// Gets or sets the default format used to display Date
			/// strings.
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
			/// Parses the given text as a Date using the default format.
			/// </summary>
			public static Date Parse(string text)
			{
				DateTime result = DateTime.ParseExact(text, _defaultFormat, CultureInfo.InvariantCulture);
				return (Date)result;
			}
		#endregion
	
		#region Today
			/// <summary>
			/// Gets Today as a Date object.
			/// </summary>
			public static Date Today
			{
				get
				{
					return new Date(DateTime.Today);
				}
			}
		#endregion
	
		#region Constructors
			/// <summary>
			/// Creates a new date by the specified year, month and day.
			/// </summary>
			public Date(int year, int month, int day):
				this(new DateTime(year, month, day))
			{
			}
			
			/// <summary>
			/// Creates a new date from a DateTime. The time information is lost.
			/// </summary>
			public Date(DateTime dateTime)
			{
				_value = dateTime.Year * 10000 + dateTime.Month * 100 + dateTime.Day;
			}
			
			/// <summary>
			/// Creates a Date object from an integer value previously got from
			/// the AsInteger property of another Date.
			/// </summary>
			public Date(int asIntegerValue)
			{
				_value = asIntegerValue;
			}
		#endregion
		
		#region Properties
			/// <summary>
			/// Gets the Year.
			/// </summary>
			public int Year
			{
				get
				{
					return _value / 10000;
				}
			}
			
			/// <summary>
			/// Gets the Month.
			/// </summary>
			public int Month
			{
				get
				{
					return _value / 100 % 100;
				}
			}
			
			/// <summary>
			/// Gets the Day.
			/// </summary>
			public int Day
			{
				get
				{
					return _value % 100;
				}
			}
			
			private int _value;
			/// <summary>
			/// Converts this Date value to an integer representation.
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
				/// Compares if this two dates are equal.
				/// </summary>
				public static bool operator == (Date date1, Date date2)
				{
					return date1._value == date2._value;
				}
				
				/// <summary>
				/// Compares if two dates are different.
				/// </summary>
				public static bool operator != (Date date1, Date date2)
				{
					return date1._value != date2._value;
				}
				
				/// <summary>
				/// Compares if the first date is less than the second date.
				/// </summary>
				public static bool operator < (Date date1, Date date2)
				{
					return date1._value < date2._value;
				}
				
				/// <summary>
				/// Compares if the first date is greater than the second.
				/// </summary>
				public static bool operator > (Date date1, Date date2)
				{
					return date1._value > date2._value;
				}
				
				/// <summary>
				/// Compares if the first date is less than or equal to the second.
				/// </summary>
				public static bool operator <= (Date date1, Date date2)
				{
					return date1._value <= date2._value;
				}
				
				/// <summary>
				/// Compares if the first date is greater than or equal to the second.
				/// </summary>
				public static bool operator >= (Date date1, Date date2)
				{
					return date1._value >= date2._value;
				}
			#endregion
			#region Implicit and explicit conversions
				/// <summary>
				/// Implicit convertion to DateTime object.
				/// </summary>
				public static implicit operator DateTime(Date date)
				{
					if (date == default(Date))
						return default(DateTime);
				
					return new DateTime(date.Year, date.Month, date.Day);
				}
				
				/// <summary>
				/// Explicit cast from DateTime to Date value.
				/// </summary>
				public static explicit operator Date(DateTime dateTime)
				{
					return new Date(dateTime);
				}
			#endregion
			#region Operator +
				/// <summary>
				/// Returns a new DateTime composed of the given date and time.
				/// </summary>
				public static DateTime operator + (Date date, Time time)
				{
					return new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, time.Millisecond);
				}

				/// <summary>
				/// Returns a DateTime composed of this date + time.
				/// </summary>
				public DateTime Add(Time time)
				{
					return this + time;
				}
			#endregion
			
			/// <summary>
			/// Gets the HashCode of the AsInteger value of this date.
			/// </summary>
			public override int GetHashCode()
			{
				return _value.GetHashCode();
			}
			
			/// <summary>
			/// Compares if this Date equals another object.
			/// </summary>
			public override bool Equals(object obj)
			{
				if (obj is Date)
				{
					Date other = (Date)obj;
					return Equals(other);
				}
				
				return base.Equals(obj);
			}
			
			/// <summary>
			/// Returns true if this Date equals another Date.
			/// </summary>
			public bool Equals(Date other)
			{
				return _value == other._value;
			}
			
			/// <summary>
			/// Converts this Date to a string representation.
			/// </summary>
			public override string ToString()
			{
				if (this == default(Date))
					return "";
			
				DateTime dateTime = this;
				return dateTime.ToString(_defaultFormat);
			}
			
			/// <summary>
			/// Compares this date to another date.
			/// </summary>
			public int CompareTo(Date other)
			{
				return _value - other._value;
			}
			
			int IComparable.CompareTo(object obj)
			{
				return CompareTo((Date)obj);
			}
		#endregion
	}
}
