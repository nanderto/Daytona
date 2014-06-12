using System;
using System.Runtime.InteropServices;
using Pfz.DataTypes;

namespace Pfz.Caching
{
	/// <summary>
	/// Interface implemented by ThreadUnsafeReference and Reference classes.
	/// This is the untyped version.
	/// </summary>
	public interface IReference:
		IReferenceData,
		IValueContainer,
		IDisposable
	{
		/// <summary>
		/// Gets or sets the HandleType of this reference.
		/// </summary>
		new GCHandleType HandleType { get; set; }

		/// <summary>
		/// Gets a copy of this reference HandleType and Value at once.
		/// </summary>
		IReferenceData GetData();

		/// <summary>
		/// Sets the HandleType and Value at once.
		/// </summary>
		void Set(GCHandleType handleType, object value);
	}
	
	/// <summary>
	/// Interface implemented by ThreadUnsafeReference and Reference classes.
	/// </summary>
	public interface IReference<T>:
		IReference,
		IReferenceData<T>,
		IValueContainer<T>
	{
		/// <summary>
		/// Gets a typed copy of HandleType and Value at once.
		/// </summary>
		/// <returns></returns>
		new ReferenceData<T> GetData();

		/// <summary>
		/// Sets HandleType and Value at once.
		/// </summary>
		void Set(GCHandleType handleType, T value);
	}
}
