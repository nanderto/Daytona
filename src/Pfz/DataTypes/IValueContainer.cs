
namespace Pfz.DataTypes
{
	/// <summary>
	/// Interface used as a read-only value container.
	/// </summary>
	public interface IReadOnlyValueContainer
	{
		/// <summary>
		/// Gets the Value of this container.
		/// </summary>
		object Value { get; }
	}
	/// <summary>
	/// Interface used as a write-only value container.
	/// </summary>
	public interface IWriteOnlyValueContainer
	{
		/// <summary>
		/// Sets the Value of this container.
		/// </summary>
		void SetValue(object value);
	}

	/// <summary>
	/// Interface used by any component that holds a value.
	/// Used by Box class and by RecordBound controls.
	/// </summary>
	public interface IValueContainer:
		IReadOnlyValueContainer,
		IWriteOnlyValueContainer
	{
	}

	/// <summary>
	/// Typed version of the read-only container.
	/// </summary>
	public interface IReadOnlyValueContainer<out T>:
		IReadOnlyValueContainer
	{
		/// <summary>
		/// Gets the Value.
		/// </summary>
		new T Value { get; }
	}

	/// <summary>
	/// Typed version of the write-only container.
	/// </summary>
	public interface IWriteOnlyValueContainer<in T>:
		IWriteOnlyValueContainer
	{
		/// <summary>
		/// Set the value of this container.
		/// </summary>
		void SetValue(T value);
	}

	/// <summary>
	/// Typed version of the value container.
	/// </summary>
	public interface IValueContainer<T>:
		IReadOnlyValueContainer<T>,
		IWriteOnlyValueContainer<T>
	{
	}
}
