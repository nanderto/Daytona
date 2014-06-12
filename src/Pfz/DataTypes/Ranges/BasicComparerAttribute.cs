using System;

namespace Pfz.DataTypes.Ranges
{
	/// <summary>
	/// Attribute that must be used by BasicComparer sub-classes.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple=false, Inherited=false)]
	public sealed class BasicComparerAttribute:
		Attribute
	{
		/// <summary>
		/// Creates a new instance of BasicComparerAttribute set to the given
		/// comparison and value to compare to.
		/// </summary>
		public BasicComparerAttribute(BasicComparison comparison, object compareToValue)
		{
			Comparison = comparison;
			CompareToValue = compareToValue;
		}
		
		/// <summary>
		/// Gets the Comparison that must be used.
		/// </summary>
		public BasicComparison Comparison { get; private set; }
		
		/// <summary>
		/// Gets the value to compare to.
		/// </summary>
		public object CompareToValue { get; private set; }
	}
}
