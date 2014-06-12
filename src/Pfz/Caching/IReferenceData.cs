using System.Runtime.InteropServices;
using Pfz.DataTypes;

namespace Pfz.Caching
{
	/// <summary>
	/// Interface used by 
	/// </summary>
	public interface IReferenceData:
		IReadOnlyValueContainer
	{
		/// <summary>
		/// Gets the HandleType or this reference-data.
		/// </summary>
		GCHandleType HandleType { get; }
	}

	/// <summary>
	/// Typed version of IReferenceData.
	/// </summary>
	public interface IReferenceData<out T>:
		IReferenceData,
		IReadOnlyValueContainer<T>
	{
	}
}
