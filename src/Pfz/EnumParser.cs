using System;

namespace Pfz
{
	/// <summary>
	/// Class that adds generic Parse methods to enums.
	/// </summary>
	public static class EnumParser
	{
		#region Parse
			/// <summary>
			/// Parses a value returning the typed enum.
			/// </summary>
			/// <typeparam name="T">The type of the enum.</typeparam>
			/// <param name="value">The value to parse.</param>
			/// <returns>The enum found.</returns>
			public static T Parse<T>(string value)
			{
				return (T)Enum.Parse(typeof(T), value);
			}

			/// <summary>
			/// Parses a value returning the typed enum.
			/// </summary>
			/// <typeparam name="T">The type of the enum.</typeparam>
			/// <param name="value">The value to parse.</param>
			/// <param name="ignoreCase">Case must be ignored or not?</param>
			/// <returns>The enum found.</returns>
			public static T Parse<T>(string value, bool ignoreCase)
			{
				return (T)Enum.Parse(typeof(T), value, ignoreCase);
			}
		#endregion
	}
}
