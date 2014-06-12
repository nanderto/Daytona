
namespace Pfz.DataTypes.Ranges
{
	/// <summary>
	/// Class for holding positive values, excluding zero.
	/// </summary>
	[BasicComparer(BasicComparison.GreaterThan, 0)]
	public sealed class PositiveWithout0<T>:
		BasicComparer<T>
	{
		/// <summary>
		/// Creates a new instance with the given value, or throws an exception
		/// if the value is invalid.
		/// </summary>
		public PositiveWithout0(T value):
			base(value)
		{
		}
	}
}
