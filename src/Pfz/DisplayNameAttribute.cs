using System;

namespace Pfz
{
	/// <summary>
	/// Allows you to se a different display name for an enum value,
	/// or simple put a DisplayName to fields, properties and other things.
	/// </summary>
	[AttributeUsage(AttributeTargets.All, AllowMultiple=false, Inherited=true)]
	public sealed class DisplayNameAttribute:
		Attribute
	{
		#region Constructor
			/// <summary>
			/// Creates the display name attribute setting it's value.
			/// </summary>
			/// <param name="displayName">The displayName to use for this enum.</param>
			public DisplayNameAttribute(string displayName)
			{
				DisplayName = displayName;
			}
		#endregion
		
		#region DisplayName
			/// <summary>
			/// Gets the display name for the enum value.
			/// </summary>
			public string DisplayName { get; private set; }
		#endregion
	}
}
