using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Pfz.Extensions;

namespace Pfz.Factories
{
	/// <summary>
	/// Class with some helper methods to the generic version of Factories.
	/// </summary>
	public sealed class Factories
	{
		#region Static Area
			/// <summary>
			/// Tries to get and wraps an editor factory.
			/// </summary>
			public static EditorFactory<DataType> TryGetEditorFactory<DataType>()
			{
				var result = Factories<IEditor>.TryGetFactory(typeof(DataType));
				if (result == null)
					return null;

				return new EditorFactory<DataType>(result);
			}

			/// <summary>
			/// Gets and wraps an editor factory.
			/// Throws an exception if an editor factory is not found.
			/// </summary>
			public static EditorFactory<DataType> GetEditorFactory<DataType>()
			{
				var result = Factories<IEditor>.GetFactory(typeof(DataType));
				return new EditorFactory<DataType>(result);
			}

			/// <summary>
			/// Tries to get and wraps a searcher factory.
			/// </summary>
			public static SearcherFactory<DataType> TryGetSearcherFactory<DataType>()
			{
				var result = Factories<ISearcher>.TryGetFactory(typeof(DataType));
				if (result == null)
					return null;

				return new SearcherFactory<DataType>(result);
			}

			/// <summary>
			/// Gets and wraps a searcher factory.
			/// Throws an exception if a facoty is not found.
			/// </summary>
			public static SearcherFactory<DataType> GetSearcherFactory<DataType>()
			{
				var result = Factories<ISearcher>.GetFactory(typeof(DataType));
				return new SearcherFactory<DataType>(result);
			}
		#endregion

		private readonly Type _factoryType;
		private readonly MethodInfo _getFactory;

		/// <summary>
		/// Creates a new Factories object, which will be made for the given baseFactoryType.
		/// </summary>
		public Factories(Type baseFactoryType)
		{
			if (baseFactoryType == null)
				throw new ArgumentNullException("baseFactoryType");

			_factoryType = typeof(Factories<>).MakeGenericType(baseFactoryType);
			_getFactory = _factoryType.GetMethod("GetFactory");
		}

		/// <summary>
		/// Gets the Factory for the given data-type.
		/// </summary>
		public IFactory GetFactory(Type dataType)
		{
			return (IFactory)_getFactory.Invoke(null, new object[]{dataType});
		}
	}

	/// <summary>
	/// Class responsible for controlling factories.
	/// </summary>
	public static class Factories<BaseFactoryType>
	where
		BaseFactoryType: class
	{
		[SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline"), SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
		static Factories()
		{
			if (!typeof(BaseFactoryType).ContainsCustomAttribute<FactoryBaseAttribute>())
				throw new ArgumentException("Factory<BaseFactoryType> can only accept interface types with [FactoryBase] as generic argument.", typeof(BaseFactoryType).FullName);

			var entryAssembly = Assembly.GetEntryAssembly();
			if (entryAssembly != null)
			{
				var referencedAssemblies = entryAssembly.GetReferencedAssemblies();
				foreach(var referencedAssembly in referencedAssemblies)
					Assembly.Load(referencedAssembly);
			}

			foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				foreach(var type in assembly.GetTypes())
				{
					var attributes = type.GetCustomAttributes<AutoRegisterInFactoryAttributeBase>();
					foreach(var attribute in attributes)
						if (attribute.BaseFactoryType == typeof(BaseFactoryType))
							Register(type, attribute.DataType, attribute.CanBeUsedForSubDataTypes);
				}
			}
		}

		internal static readonly TypeDictionary<ConstructorInfo> _typeDictionary = new TypeDictionary<ConstructorInfo>();

		/// <summary>
		/// Registers a type to be created for the given datatype.
		/// </summary>
		public static void Register(Type typeToCreate, Type dataType, bool canCreateForSubDataTypes=false)
		{
			if (typeToCreate == null)
				throw new ArgumentNullException("typeToCreate");

			if (dataType == null)
				throw new ArgumentNullException("dataType");

			if (!typeof(BaseFactoryType).IsAssignableFrom(typeToCreate))
				throw new ArgumentException(typeToCreate.FullName + " must implement " + typeof(BaseFactoryType).FullName + ".", "typeToCreate");

			var constructorInfo = typeToCreate.GetConstructor(Type.EmptyTypes);
			if (constructorInfo == null)
				throw new ArgumentException(typeToCreate.FullName + " does not have a public default constructor.", "typeToCreate");

			_typeDictionary.Set(dataType, constructorInfo, canCreateForSubDataTypes);
		}

		/// <summary>
		/// Tries to register a factory for the given dataType.
		/// This will not unregister editors for parent dataTypes capable of editing sub-types.
		/// </summary>
		public static bool Unregister(Type dataType)
		{
			return _typeDictionary.Remove(dataType);
		}

		/// <summary>
		/// Tries to get a factory for the given dataType.
		/// </summary>
		public static Factory<BaseFactoryType> TryGetFactory(Type dataType)
		{
			if (dataType == null)
				throw new ArgumentNullException("dataType");

			var constructorInfo = _typeDictionary.FindUpOrDefault(dataType);
			if (constructorInfo == null)
				return null;

			Type declaringType = constructorInfo.DeclaringType;
			if (declaringType.ContainsGenericParameters)
			{
				Type madeType = declaringType.MakeGenericType(dataType);
				constructorInfo = madeType.GetConstructor(Type.EmptyTypes);
			}

			return new Factory<BaseFactoryType>(constructorInfo, dataType);
		}

		/// <summary>
		/// Gets a factory for the given dataType or throws an exception.
		/// </summary>
		public static Factory<BaseFactoryType> GetFactory(Type dataType)
		{
			if (dataType == null)
				throw new ArgumentNullException("dataType");

			Factory<BaseFactoryType> result = TryGetFactory(dataType);

			if (result == null)
				throw new ArgumentException("Can't find a factory of base type " + typeof(BaseFactoryType).FullName + " for data type " + dataType.FullName + ".", "dataType");

			return result;
		}
	}
}
