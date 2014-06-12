using System;

namespace Pfz.DataTypes
{
	/// <summary>
	/// Generic event args, so you can pass different typed parameters to it 
	/// without creating sub-classes.
	/// </summary>
	/// <typeparam name="T">The type of the value that will be holded.</typeparam>
	[Serializable]
	public class EventArgs<T>:
		EventArgs,
		IValueContainer<T>
	{
		/// <summary>
		/// Gets or sets the value of this EventArgs class.
		/// </summary>
		public T Value { get; set; }

		#region IValueContainer Members
			object IReadOnlyValueContainer.Value
			{
				get
				{
					return Value;
				}
			}
			void IWriteOnlyValueContainer.SetValue(object value)
			{
				Value = (T)value;
			}
			void IWriteOnlyValueContainer<T>.SetValue(T value)
			{
				Value = value;
			}
		#endregion
	}
}
