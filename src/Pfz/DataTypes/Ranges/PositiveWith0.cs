
namespace Pfz.DataTypes.Ranges
{
	/// <summary>
	/// Class for holding positive values, including zero.
	/// </summary>
	[BasicComparer(BasicComparison.GreaterThanOrEqualTo, 0)]
	public sealed class PositiveWith0<T>:
		BasicComparer<T>
	{
		/// <summary>
		/// Creates a new instance with the given value, or throws an exception
		/// if the value is invalid.
		/// </summary>
		public PositiveWith0(T value):
			base(value)
		{
		}
	}
}
