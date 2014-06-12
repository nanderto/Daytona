using System;
using System.ComponentModel;
using System.Globalization;

namespace Pfz.DataTypes
{
	/// <summary>
	/// A converter for the Type.
	/// It converts the type to and from strings.
	/// </summary>
	public class TypeTypeConverter:
		TypeConverter
	{
		/// <summary>
		/// Returns true for strings.
		/// </summary>
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return sourceType == typeof(string);
		}

		/// <summary>
		/// Returns true for strings.
		/// </summary>
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return destinationType == typeof(string);
		}
		
		/// <summary>
		/// Gets a type by it's full type name.
		/// </summary>
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			string strValue = value as string;
			return ConvertFrom(strValue);
		}

		/// <summary>
		/// Gets a type by it's full type name.
		/// </summary>
		public static Type ConvertFrom(string value)
		{
			if (string.IsNullOrEmpty(value))
				return null;

			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var assembly in assemblies)
			{
				Type type = assembly.GetType(value, false);
				if (type != null)
					return type;
			}

			throw new ArgumentException("Type " + value + " not found.");
		}
		
		/// <summary>
		/// Gets the full name of a type.
		/// </summary>
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			var recordType = value as Type;
			if (recordType == null)
				return null;
			
			return recordType.FullName;
		}
	}
}
