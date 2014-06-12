
namespace Pfz.DataTypes.Ranges
{
	/// <summary>
	/// Class for holding negative values, excluding zero.
	/// </summary>
	[BasicComparer(BasicComparison.LessThan, 0)]
	public sealed class NegativeWithout0<T>:
		BasicComparer<T>
	{
		/// <summary>
		/// Creates a new instance with the given value, or throws an exception
		/// if the value is invalid.
		/// </summary>
		public NegativeWithout0(T value):
			base(value)
		{
		}
	}
}
