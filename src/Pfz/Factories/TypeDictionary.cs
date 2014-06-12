using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Pfz.Extensions;

namespace Pfz.Factories
{
	/// <summary>
	/// This class is a special dictionary for types, which can also 
	/// search values that are set for base types or interfaces.
	/// </summary>
	public sealed class TypeDictionary<TValue>
	{
		private Dictionary<Type, TValue> _exactTypes = new Dictionary<Type, TValue>();
		private Dictionary<Type, TValue> _inheritedTypes = new Dictionary<Type, TValue>();
		private Dictionary<Type, TValue> _interfaceTypes = new Dictionary<Type, TValue>();
		private Dictionary<Type, TValue> _exactGenericTypes = new Dictionary<Type, TValue>();
		private Dictionary<Type, TValue> _inheritedGenericTypes = new Dictionary<Type, TValue>();
		private Dictionary<Type, TValue> _genericInterfaceTypes = new Dictionary<Type, TValue>();
		
		/// <summary>
		/// Gets a value for a type, without doing a "search".
		/// </summary>
		public TValue this[Type type]
		{
			get
			{
				if (type == null)
					throw new ArgumentNullException("type");

				TValue result;
				
				if (type.IsGenericTypeDefinition)
				{
					if (_exactGenericTypes.TryGetValue(type, out result))
						return result;
						
					if (type.IsInterface)
						return _genericInterfaceTypes[type];
					
					return _inheritedGenericTypes[type];
				}

				if (_exactTypes.TryGetValue(type, out result))
					return result;

				if (type.IsInterface)
					return _interfaceTypes[type];

				return _inheritedTypes[type];
			}
		}
		
		/// <summary>
		/// Clears all items in this dictionary.
		/// </summary>
		public void Clear()
		{
			_exactTypes.Clear();
			_inheritedTypes.Clear();
			_interfaceTypes.Clear();
			_exactGenericTypes.Clear();
			_inheritedGenericTypes.Clear();
			_genericInterfaceTypes.Clear();
		}
		
		/// <summary>
		/// Removes a value for a type.
		/// </summary>
		public bool Remove(Type type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
		
			if (type.IsGenericTypeDefinition)
			{
				if (_exactGenericTypes.Remove(type))
					return true;
				
				if (type.IsInterface)
					return _genericInterfaceTypes.Remove(type);
					
				return _inheritedGenericTypes.Remove(type);
			}
			
			if (_exactTypes.Remove(type))
				return true;
			
			if (type.IsInterface)
				return _interfaceTypes.Remove(type);
			
			return _inheritedTypes.Remove(type);
		}
		
		/// <summary>
		/// Sets a value for the given type.
		/// Register is as exact match or not by the boolean value.
		/// </summary>
		public void Set(Type type, TValue value, bool isInheritable)
		{
			if (isInheritable)
				SetAsInheritable(type, value);
			else
				SetAsExactMatch(type, value);
		}
		
		/// <summary>
		/// Set the value for a given type, but consider the type only as exact match,
		/// so it will not be found as a base type in FindUp.
		/// </summary>
		public void SetAsExactMatch(Type type, TValue value)
		{
			if (type == null)
				throw new ArgumentNullException("type");
				
			if (type.IsGenericTypeDefinition)
				_exactGenericTypes[type] = value;
			else
				_exactTypes[type] = value;
		}
		
		/// <summary>
		/// Sets the value for a given type, and tells that such value can be
		/// used by sub-types if one more appropriate is not found.
		/// </summary>
		public void SetAsInheritable(Type type, TValue value)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			
			if (type.IsGenericTypeDefinition)
			{
				//if (type.IsSealed)
				//	fExactGenericTypes[type] = value;
				//else
				// even if this type is sealed, it must not be used as exact,
				// as something registered for List<> can also work with List<string>,
				// by default.
				
				if (type.IsInterface)
					_genericInterfaceTypes[type] = value;
				else
					_inheritedGenericTypes[type] = value;
			}
			else
			{
				if (type.IsSealed)
					_exactTypes[type] = value;
				else
				if (type.IsInterface)
					_interfaceTypes[type] = value;
				else
					_inheritedTypes[type] = value;
			}
		}
		
		/// <summary>
		/// Finds a value for the actual type or for a parent type.
		/// Returns the default value if nothing is found.
		/// </summary>
		public TValue FindUpOrDefault(Type type)
		{
			TValue result;
			TryFindUp(type, out result);
			return result;
		}
		
		/// <summary>
		/// Tries to find the value for the actual type or for a parent type.
		/// Returns true if the value is found, false otherwise.
		/// </summary>
		[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		public bool TryFindUp(Type type, out TValue value)
		{
			if (type == null)
				throw new ArgumentNullException("type");
		
			if (type.IsGenericTypeDefinition)
			{
				if (_exactGenericTypes.TryGetValue(type, out value))
					return true;
			}
			else
			{
				if (_exactTypes.TryGetValue(type, out value))
					return true;

				if (type.IsGenericType)
				{
					// For example, here we can have the List<string> type.
					// But, before searching for a registered type for List<>
					// We need to check if we don't have such type registered in
					// the inheritance dictionary.
				
					if (type.IsInterface)
					{
						if (_interfaceTypes.TryGetValue(type, out value))
							return true;
					}
					else
					{
						if (_inheritedTypes.TryGetValue(type, out value))
							return true;
					}

					Type typeDefinition = type.GetGenericTypeDefinition();
					if (_exactGenericTypes.TryGetValue(typeDefinition, out value))
						return true;
				}
			}
			
			if (type.IsInterface)
			{
				// In one of the following blocks we will get the interfaces of the actual type.
				// But, if the actual type is an interface also, getting it's interfaces
				// will not get the type itself, so we must check for it.
				if (type.IsGenericTypeDefinition)
				{
					if (_genericInterfaceTypes.TryGetValue(type, out value))
						return true;
				}
				else
				{
					if (_interfaceTypes.TryGetValue(type, out value))
						return true;

					if (type.IsGenericType)
					{
						Type typeDefinition = type.GetGenericTypeDefinition();
						if (_genericInterfaceTypes.TryGetValue(typeDefinition, out value))
							return true;
					}
				}
			}
			else
			{
				Type baseType = type;
				
				// we already checked for the first item it the type is a generic one,
				// so we don't do it again.
				if (type.IsGenericType && !type.IsGenericTypeDefinition)
					baseType = type.BaseType;
				
				while(baseType != null)
				{
					if (baseType.IsGenericTypeDefinition)
					{
						if (_inheritedGenericTypes.TryGetValue(baseType, out value))
							return true;
					}
					else
					{
						if (_inheritedTypes.TryGetValue(baseType, out value))
							return true;

						if (baseType.IsGenericType)
						{
							Type typeDefinition = baseType.GetGenericTypeDefinition();
							if (_inheritedGenericTypes.TryGetValue(typeDefinition, out value))
								return true;
						}
					}
					
					baseType = baseType.BaseType;
				}
			}
			
			var orderedInterfaces = type.GetOrderedInterfaces();
			foreach(Type interfaceType in orderedInterfaces)
			{
				if (_interfaceTypes.TryGetValue(interfaceType, out value))
					return true;

				if (interfaceType.IsGenericType)
				{
					Type genericTypeDefinition = interfaceType.GetGenericTypeDefinition();
					if (_genericInterfaceTypes.TryGetValue(genericTypeDefinition, out value))
						return true;
				}
			}

			if (type.IsInterface)
				return _inheritedTypes.TryGetValue(typeof(object), out value);
			
			return false;
		}
	}
}
