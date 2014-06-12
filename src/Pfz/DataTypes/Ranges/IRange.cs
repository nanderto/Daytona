using System;

namespace Pfz.DataTypes.Ranges
{
	/// <summary>
	/// Interface used by the Range class, so you can get values without knowing the
	/// exact data-type of the range.
	/// </summary>
	public interface IRange:
		IValueContainer
	{
		/// <summary>
		/// Gets the DataType of the values supported by this range.
		/// </summary>
		Type DataType { get; }
		
		/// <summary>
		/// Gets the minimum value allowed by this range.
		/// </summary>
		object MinimumValue { get; }
		
		/// <summary>
		/// Gets the maximum value allowed by this range.
		/// </summary>
		object MaximumValue { get; }
	}
}
