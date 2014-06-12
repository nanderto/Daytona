using System;
using System.Reflection;

namespace Pfz.Extensions
{
	/// <summary>
	/// Adds methods to work easily with Enums.
	/// </summary>
	public static class PfzDisplayNameExtensions
	{
		#region GetDisplayName
			/// <summary>
			/// Gets the display name of an enumerated value.
			/// If no EnumDisplayName attribute is set, uses the default enum name.
			/// </summary>
			/// <param name="enumValue">The enum value to get the display name.</param>
			/// <returns>The display name.</returns>
			public static string GetDisplayName(this Enum enumValue)
			{
				if (enumValue == null)
					return null;
			
				string name = enumValue.ToString();
				FieldInfo fieldInfo = enumValue.GetType().GetField(name);
				
				if (fieldInfo == null)
					return name;
					
				var attribute = fieldInfo.GetCustomAttribute<DisplayNameAttribute>();
				
				if (attribute != null)
					return attribute.DisplayName;

				return name;
			}
			
			/// <summary>
			/// Gets the DisplayName of a member, or it's real name if it does
			/// not have a DisplayName.
			/// </summary>
			/// <param name="memberInfo">The member to get the display name for.</param>
			/// <returns>A name.</returns>
			public static string GetDisplayName(this MemberInfo memberInfo)
			{
				if (memberInfo == null)
					return null;

				if (memberInfo.MemberType == MemberTypes.Property)
				{
					PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
					return propertyInfo.GetDisplayName();
				}

				var attribute = memberInfo.GetCustomAttribute<DisplayNameAttribute>();
				if (attribute == null)
					return memberInfo.Name;

				return attribute.DisplayName;
			}

			/// <summary>
			/// Gets the DisplayName of the given property.
			/// If the property does not have a display name and has the same name of its
			/// PropertyType, then the DisplayName of the PropertyType is used.
			/// </summary>
			public static string GetDisplayName(this PropertyInfo propertyInfo)
			{
				if (propertyInfo == null)
					return null;

				var attribute = propertyInfo.GetCustomAttribute<DisplayNameAttribute>();
				if (attribute != null)
					return attribute.DisplayName;

				Type propertyType = propertyInfo.PropertyType;
				string name = propertyInfo.Name;
				if (name == propertyType.Name)
					return propertyType.GetDisplayName();

				return name;
			}
		#endregion
	}
}
