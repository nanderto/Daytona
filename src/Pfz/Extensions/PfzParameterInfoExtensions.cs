using System;
using System.Reflection;

namespace Pfz.Extensions
{
	/// <summary>
	/// Adds the GetTypes method to a parameterInfo array.
	/// </summary>
	public static class PfzParameterInfoExtensions
	{
		/// <summary>
		/// Returns an arrays that contains all the parameterTypes of the parameterInfos array.
		/// </summary>
		public static Type[] GetTypes(this ParameterInfo[] parameterInfos)
		{
			if (parameterInfos == null)
				throw new ArgumentNullException("parameterInfos");

			Type[] result = new Type[parameterInfos.Length];
			for (int i=0; i<parameterInfos.Length; i++)
				result[i] = parameterInfos[i].ParameterType;

			return result;
		}
	}
}
