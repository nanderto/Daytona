using System;

namespace Pfz.DataTypes.Ranges
{
	/// <summary>
	/// Attribute that must be used in Range classes to tell which range is valid.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple=false, Inherited=false)]
	public sealed class RangeAttribute:
		Attribute
	{
		/// <summary>
		/// Creates a new instance of the RangeAttribute setting the minimum and
		/// maximum values.
		/// </summary>
		public RangeAttribute(object minimumValue, object maximumValue)
		{
			MinimumValue = minimumValue;
			MaximumValue = maximumValue;
		}
		
		/// <summary>
		/// Gets the MinimumValue of this range.
		/// </summary>
		public object MinimumValue { get; private set; }

		/// <summary>
		/// Gets the MinimumValue of this range.
		/// </summary>
		public object MaximumValue { get; private set; }
	}
}
