using System;

namespace Pfz.DataTypes.Ranges
{
	/// <summary>
	/// Interface implemented by BasicComparer class so you can use it without
	/// knowing it's data-type.
	/// </summary>
	public interface IBasicComparer:
		IValueContainer
	{
		/// <summary>
		/// Gets the DataType of this comparer.
		/// </summary>
		Type DataType { get; }
		
		/// <summary>
		/// Gets the comparison used by this comparer.
		/// </summary>
		BasicComparison Comparison { get; }
		
		/// <summary>
		/// Gets the value to compare to.
		/// </summary>
		object CompareToValue { get; }
	}
}
