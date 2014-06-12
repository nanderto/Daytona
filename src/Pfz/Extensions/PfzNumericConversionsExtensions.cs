using System;
using System.Text;

namespace Pfz.Extensions.NumericConversionsExtensions
{
	/// <summary>
	/// Class that has methods to convert values to and from many different
	/// representations.
	/// </summary>
	public static class PfzNumericConversionsExtensions
	{
		#region 64 bit
			/// <summary>
			/// Converts a value to string using the specific numericBase.
			/// </summary>
			/// <param name="value">The value to generate a string representation.</param>
			/// <param name="numericBase">The numericBase to use. 2 is binary, 16 is hexadecimal and so on.</param>
			/// <returns>An string representation of the given value using the right numericBase.</returns>
			[CLSCompliant(false)]
			public static string ToString(this ulong value, byte numericBase)
			{
				if (numericBase < 2 || numericBase > 36)
					throw new ArgumentException("numericBase must be between 2 and 36.", "numericBase");
					
				if (value == 0)
					return "0";
			
				StringBuilder result = new StringBuilder();
				while(value > 0)
				{
					char c;
				
					byte mod = (byte)(value % numericBase);
					value /= numericBase;
					
					if (mod < 10)
						c = (char)('0' + mod);
					else
						c = (char)('a' - 10 + mod);
						
					result.Insert(0, c);
				}
				return result.ToString();
			}
			
			/// <summary>
			/// Converts a value to string using the specific numericBase.
			/// </summary>
			/// <param name="value">The value to generate a string representation.</param>
			/// <param name="numericBase">The numericBase to use. 2 is binary, 16 is hexadecimal and so on.</param>
			/// <returns>An string representation of the given value using the right numericBase.</returns>
			public static string ToString(this long value, byte numericBase)
			{
				if (numericBase < 2 || numericBase > 36)
					throw new ArgumentException("numericBase must be between 2 and 36.", "numericBase");
					
				if (value == 0)
					return "0";
			
				StringBuilder result = new StringBuilder();
				
				if (value < 0)
				{
					result.Append('-');
					while(value < 0)
					{
						char c;
					
						byte mod = (byte)(-(value % numericBase));
						value /= numericBase;
						
						if (mod < 10)
							c = (char)('0' + mod);
						else
							c = (char)('a' - 10 + mod);
							
						result.Insert(1, c);
					}
				}
				else
				{
					while(value > 0)
					{
						char c;
					
						byte mod = (byte)(value % numericBase);
						value /= numericBase;
						
						if (mod < 10)
							c = (char)('0' + mod);
						else
							c = (char)('a' - 10 + mod);
							
						result.Insert(0, c);
					}
				}
				
				return result.ToString();
			}

			/// <summary>
			/// Tries to converts an string into a ulong representation using an specific 
			/// numericBase.
			/// </summary>
			/// <param name="value">The string representation to convert to an ulong.</param>
			/// <param name="numericBase">The numericBase to use. For example, 2 is binary.</param>
			/// <param name="result">The resulting value.</param>
			/// <returns>true if the convertion was possible.</returns>
			[CLSCompliant(false)]
			public static bool TryParseUInt64(this string value, byte numericBase, out ulong result)
			{
				if (numericBase < 2 || numericBase > 36)
					throw new ArgumentException("numericBase must be between 2 and 36.", "numericBase");
					
				result = 0;
				if (string.IsNullOrEmpty(value))
					return false;
				
				foreach(char c in value)
				{
					result *= numericBase;
					
					byte cValue;
					if (c >= '0' && c <= '9')
						cValue = (byte)(c - '0');
					else
					if (c >= 'a' && c <= 'z')
						cValue = (byte)(c - 'a' + 10);
					else
					if (c >= 'A' && c <= 'Z')
						cValue = (byte)(c - 'A' + 10);
					else
						cValue = 255;

					if (cValue >= numericBase)
					{					
						result = 0;
						return false;
					}
					
					result += cValue;
				}
				
				return true;
			}
			
			/// <summary>
			/// Parses an string into an ulong using the given numericBase.
			/// </summary>
			/// <param name="value">The value to convert.</param>
			/// <param name="numericBase">The numericBase to use. For example, 2 is binary.</param>
			/// <returns>An ulong or throws an exception.</returns>
			[CLSCompliant(false)]
			public static ulong ParseUInt64(this string value, byte numericBase)
			{
				ulong result;
				if (!TryParseUInt64(value, numericBase, out result))
					throw new FormatException("Invalid value " + value + " for base " + numericBase + ".");
				
				return result;
			}

			/// <summary>
			/// Tries to converts an string into a ulong representation using an specific 
			/// numericBase.
			/// </summary>
			/// <param name="value">The string representation to convert to an ulong.</param>
			/// <param name="numericBase">The numericBase to use. For example, 2 is binary.</param>
			/// <param name="result">The resulting value.</param>
			/// <returns>true if the convertion was possible.</returns>
			public static bool TryParseInt64(this string value, byte numericBase, out long result)
			{
				if (numericBase < 2 || numericBase > 36)
					throw new ArgumentException("numericBase must be between 2 and 36.", "numericBase");
					
				result = 0;
				if (string.IsNullOrEmpty(value))
					return false;
				
				int startIndex = 0;
				int count = value.Length;
				if (value[0] == '-')
				{
					if (count == 1)
						return false;

					startIndex = 1;
				}

				for (int i=startIndex; i<count; i++)
				{
					char c = value[i];

					result *= numericBase;
					
					byte cValue;
					if (c >= '0' && c <= '9')
						cValue = (byte)(c - '0');
					else
					if (c >= 'a' && c <= 'z')
						cValue = (byte)(c - 'a' + 10);
					else
					if (c >= 'A' && c <= 'Z')
						cValue = (byte)(c - 'A' + 10);
					else
						cValue = 255;

					if (cValue >= numericBase)
					{					
						result = 0;
						return false;
					}
					
					result += cValue;
				}
				
				if (startIndex == 1)
					result = -result;
				
				return true;
			}
			
			/// <summary>
			/// Parses an string into an ulong using the given numericBase.
			/// </summary>
			/// <param name="value">The value to convert.</param>
			/// <param name="numericBase">The numericBase to use. For example, 2 is binary.</param>
			/// <returns>An ulong or throws an exception.</returns>
			public static long ParseInt64(this string value, byte numericBase)
			{
				long result;
				if (!TryParseInt64(value, numericBase, out result))
					throw new FormatException("Invalid value " + value + " for base " + numericBase + ".");
				
				return result;
			}
		#endregion
		#region 32 bit
			/// <summary>
			/// Converts a value to string using the specific numericBase.
			/// </summary>
			/// <param name="value">The value to generate a string representation.</param>
			/// <param name="numericBase">The numericBase to use. 2 is binary, 16 is hexadecimal and so on.</param>
			/// <returns>An string representation of the given value using the right numericBase.</returns>
			[CLSCompliant(false)]
			public static string ToString(this uint value, byte numericBase)
			{
				if (numericBase < 2 || numericBase > 36)
					throw new ArgumentException("numericBase must be between 2 and 36.", "numericBase");
					
				if (value == 0)
					return "0";
			
				StringBuilder result = new StringBuilder();
				while(value > 0)
				{
					char c;
				
					byte mod = (byte)(value % numericBase);
					value /= numericBase;
					
					if (mod < 10)
						c = (char)('0' + mod);
					else
						c = (char)('a' - 10 + mod);
						
					result.Insert(0, c);
				}
				return result.ToString();
			}
			
			/// <summary>
			/// Converts a value to string using the specific numericBase.
			/// </summary>
			/// <param name="value">The value to generate a string representation.</param>
			/// <param name="numericBase">The numericBase to use. 2 is binary, 16 is hexadecimal and so on.</param>
			/// <returns>An string representation of the given value using the right numericBase.</returns>
			public static string ToString(this int value, byte numericBase)
			{
				if (numericBase < 2 || numericBase > 36)
					throw new ArgumentException("numericBase must be between 2 and 36.", "numericBase");
					
				if (value == 0)
					return "0";
			
				StringBuilder result = new StringBuilder();
				
				if (value < 0)
				{
					result.Append('-');
					while(value < 0)
					{
						char c;
					
						byte mod = (byte)(-(value % numericBase));
						value /= numericBase;
						
						if (mod < 10)
							c = (char)('0' + mod);
						else
							c = (char)('a' - 10 + mod);
							
						result.Insert(1, c);
					}
				}
				else
				{
					while(value > 0)
					{
						char c;
					
						byte mod = (byte)(value % numericBase);
						value /= numericBase;
						
						if (mod < 10)
							c = (char)('0' + mod);
						else
							c = (char)('a' - 10 + mod);
							
						result.Insert(0, c);
					}
				}
				return result.ToString();
			}

			/// <summary>
			/// Tries to converts an string into a uint representation using an specific 
			/// numericBase.
			/// </summary>
			/// <param name="value">The string representation to convert to an uint.</param>
			/// <param name="numericBase">The numericBase to use. For example, 2 is binary.</param>
			/// <param name="result">The resulting value.</param>
			/// <returns>true if the convertion was possible.</returns>
			[CLSCompliant(false)]
			public static bool TryParseUInt32(this string value, byte numericBase, out uint result)
			{
				if (numericBase < 2 || numericBase > 36)
					throw new ArgumentException("numericBase must be between 2 and 36.", "numericBase");
					
				result = 0;
				if (string.IsNullOrEmpty(value))
					return false;
				
				foreach(char c in value)
				{
					result *= numericBase;
					
					byte cValue;
					if (c >= '0' && c <= '9')
						cValue = (byte)(c - '0');
					else
					if (c >= 'a' && c <= 'z')
						cValue = (byte)(c - 'a' + 10);
					else
					if (c >= 'A' && c <= 'Z')
						cValue = (byte)(c - 'A' + 10);
					else
						cValue = 255;

					if (cValue >= numericBase)
					{					
						result = 0;
						return false;
					}
					
					result += cValue;
				}
				
				return true;
			}
			
			/// <summary>
			/// Parses an string into an uint using the given numericBase.
			/// </summary>
			/// <param name="value">The value to convert.</param>
			/// <param name="numericBase">The numericBase to use. For example, 2 is binary.</param>
			/// <returns>An uint or throws an exception.</returns>
			[CLSCompliant(false)]
			public static uint ParseUInt32(this string value, byte numericBase)
			{
				uint result;
				if (!TryParseUInt32(value, numericBase, out result))
					throw new FormatException("Invalid value " + value + " for base " + numericBase + ".");
				
				return result;
			}

			/// <summary>
			/// Tries to converts an string into a uint representation using an specific 
			/// numericBase.
			/// </summary>
			/// <param name="value">The string representation to convert to an uint.</param>
			/// <param name="numericBase">The numericBase to use. For example, 2 is binary.</param>
			/// <param name="result">The resulting value.</param>
			/// <returns>true if the convertion was possible.</returns>
			public static bool TryParseInt32(this string value, byte numericBase, out int result)
			{
				if (numericBase < 2 || numericBase > 36)
					throw new ArgumentException("numericBase must be between 2 and 36.", "numericBase");
					
				result = 0;
				if (string.IsNullOrEmpty(value))
					return false;
				
				int startIndex = 0;
				int count = value.Length;
				if (value[0] == '-')
				{
					if (count == 1)
						return false;

					startIndex = 1;
				}

				for (int i=startIndex; i<count; i++)
				{
					char c = value[i];

					result *= numericBase;
					
					byte cValue;
					if (c >= '0' && c <= '9')
						cValue = (byte)(c - '0');
					else
					if (c >= 'a' && c <= 'z')
						cValue = (byte)(c - 'a' + 10);
					else
					if (c >= 'A' && c <= 'Z')
						cValue = (byte)(c - 'A' + 10);
					else
						cValue = 255;

					if (cValue >= numericBase)
					{					
						result = 0;
						return false;
					}
					
					result += cValue;
				}
				
				if (startIndex == 1)
					result = -result;
				
				return true;
			}
			
			/// <summary>
			/// Parses an string into an uint using the given numericBase.
			/// </summary>
			/// <param name="value">The value to convert.</param>
			/// <param name="numericBase">The numericBase to use. For example, 2 is binary.</param>
			/// <returns>An uint or throws an exception.</returns>
			public static int ParseInt32(this string value, byte numericBase)
			{
				int result;
				if (!TryParseInt32(value, numericBase, out result))
					throw new FormatException("Invalid value " + value + " for base " + numericBase + ".");
				
				return result;
			}
		#endregion
	}
}
