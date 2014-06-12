using System;
using System.Collections.Generic;
using System.Reflection;

namespace Pfz.Extensions
{
	/// <summary>
	/// Adds the GetParameterTypes method to the MethodInfo class.
	/// </summary>
	public static class PfzMethodInfoExtensions
	{
		private static readonly Dictionary<MethodBase, Type[]> _parameterTypes = new Dictionary<MethodBase, Type[]>();

		/// <summary>
		/// Gets an array with the parameter types of this method info.
		/// </summary>
		public static Type[] GetParameterTypes(this MethodBase methodInfo)
		{
			if (methodInfo == null)
				throw new ArgumentNullException("methodInfo");

			Type[] result;
			lock(_parameterTypes)
			{
				if (!_parameterTypes.TryGetValue(methodInfo, out result))
				{
					var parameters = methodInfo.GetParameters();

					result = new Type[parameters.Length];
					for(int i=0; i<parameters.Length; i++)
						result[i] = parameters[i].ParameterType;

					_parameterTypes.Add(methodInfo, result);
				}
			}

			if (result.Length == 0)
				return result;

			return result.TypedClone();
		}
	}
}
