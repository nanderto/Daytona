
namespace Pfz.DataTypes.Ranges
{
	/// <summary>
	/// Class for holding negative values, including zero.
	/// </summary>
	[BasicComparer(BasicComparison.LessThanOrEqualTo, 0)]
	public sealed class NegativeWith0<T>:
		BasicComparer<T>
	{
		/// <summary>
		/// Creates a new instance with the given value, or throws an exception
		/// if the value is invalid.
		/// </summary>
		public NegativeWith0(T value):
			base(value)
		{
		}
	}
}
