using System;

namespace Pfz.DataTypes.Ranges
{
	/// <summary>
	/// This class represents values ranging from 0 to 100.
	/// </summary>
	[Range(0, 100)]
	[Serializable]
	public sealed class Range0_100<T>:
		Range<T>
	where
		T: IComparable<T>
	{
		/// <summary>
		/// Creates a new Range instance holding the given value.
		/// </summary>
		public Range0_100(T value):
			base(value)
		{
		}
	}
}
