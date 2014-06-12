using System;
using System.Reflection;
using Pfz.Extensions;

namespace Pfz.DataTypes
{
	/// <summary>
	/// Class to be used when adding an error specific to one property.
	/// </summary>
	[Serializable]
	public sealed class PropertyError
	{
		private static string _defaultMessageFormat = "Error on property \"{0}\": {1}";
		
		/// <summary>
		/// Gets or sets the format used by default when generating the
		/// string of this error.
		/// </summary>
		public static string DefaultMessageFormat
		{
			get
			{
				return _defaultMessageFormat;
			}
			set
			{
				_defaultMessageFormat = value;
			}
		}
	
		/// <summary>
		/// Creates the error object.
		/// </summary>
		public PropertyError(PropertyInfo propertyInfo, object error)
		{
			Property = propertyInfo;
			Error = error;
			MessageFormat = _defaultMessageFormat;
		}
		
		/// <summary>
		/// Gets the property that has an error.
		/// </summary>
		public PropertyInfo Property { get; private set; }
		
		/// <summary>
		/// Gets the error.
		/// </summary>
		public object Error { get; private set; }
		
		/// <summary>
		/// Gets or sets the format to generate the error message on ToString.
		/// {0} is the DisplayName of the property.
		/// {1} is the error object or, if the error is an exception, the
		/// exception.Message.
		/// </summary>
		public string MessageFormat { get; set; }

		/// <summary>
		/// Shows the error message.
		/// </summary>
		public override string ToString()
		{
			string message;
			
			Exception exception = Error as Exception;
			if (exception != null)
				message = exception.Message;
			else
				message = Error.ToString();
			
			return string.Format(MessageFormat, Property.GetDisplayName(), message);
		}
	}
}
