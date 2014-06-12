using System;
using System.Reflection;
using Pfz.Extensions;

namespace Pfz.Factories
{
	/// <summary>
	/// Class with some wrappers methods to use the generic version a litte easier.
	/// </summary>
	public static class Factory
	{
		/// <summary>
		/// Tries to create an editor and wraps it so the casts are done for you.
		/// </summary>
		public static Editor<DataType> TryCreateEditor<DataType>()
		{
			var result = Factory<IEditor>.TryCreate(typeof(DataType));
			if (result == null)
				return null;

			return new Editor<DataType>(result);
		}

		/// <summary>
		/// Create an editor and wraps it so the casts are done for you.
		/// </summary>
		public static Editor<DataType> CreateEditor<DataType>()
		{
			var result = Factory<IEditor>.Create(typeof(DataType));
			return new Editor<DataType>(result);
		}

		/// <summary>
		/// Tries to create a searcher and wraps it so the casts are done for you.
		/// </summary>
		public static Searcher<DataType> TryCreateSearcher<DataType>()
		{
			var result = Factory<ISearcher>.TryCreate(typeof(DataType));
			if (result == null)
				return null;

			return new Searcher<DataType>(result);
		}

		/// <summary>
		/// Creates a searcher and wraps it so the casts are done for you.
		/// </summary>
		public static Searcher<DataType> CreateSearcher<DataType>()
		{
			var result = Factory<ISearcher>.Create(typeof(DataType));
			return new Searcher<DataType>(result);
		}
	}

	/// <summary>
	/// This class represents a factory, so it can create many instances of its DataType.
	/// Also, see the static methods.
	/// </summary>
	public sealed class Factory<T>:
		IFactory
	where
		T: class
	{
		#region Static Area
			/// <summary>
			/// Tries to create the appropriate editor/searcher for the given dataType.
			/// </summary>
			public static T TryCreate(Type dataType)
			{
				if (dataType == null)
					throw new ArgumentNullException("dataType");

				var constructorInfo = Factories<T>._typeDictionary.FindUpOrDefault(dataType);
				if (constructorInfo == null)
					return null;

				Type declaringType = constructorInfo.DeclaringType;
				if (declaringType.ContainsGenericParameters)
				{
					Type madeType = declaringType.MakeGenericType(dataType);
					constructorInfo = madeType.GetConstructor(Type.EmptyTypes);
				}

				object result = constructorInfo.Invoke(null);

				IHasPreferredDataType preferredDataType = result as IHasPreferredDataType;
				if (preferredDataType != null)
					preferredDataType.PreferredDataType = dataType;

				return (T)result;
			}

			/// <summary>
			/// Creates the appropriate editor/searcher for the given dataType.
			/// Throws an exception if that's not possible.
			/// </summary>
			public static T Create(Type dataType)
			{
				if (dataType == null)
					throw new ArgumentNullException("dataType");

				T result = TryCreate(dataType);

				if (result == null)
					throw new ArgumentException("Can't create an " + typeof(T).FullName + " object for data type " + dataType.FullName + ".", "dataType");

				return result;
			}
		#endregion


		private ConstructorInfo _constructor;
		internal Factory(ConstructorInfo constructor, Type dataType)
		{
			_constructor = constructor;
			DataType = dataType;
		}

		/// <summary>
		/// Gets the DataType of this factory.
		/// </summary>
		public Type DataType { get; private set; }

		/// <summary>
		/// Creates a new editor/searcher for the DataType of this factory.
		/// </summary>
		public T Create()
		{
			object result = _constructor.Invoke(null);

			var preferredDataType = result as IHasPreferredDataType;
			if (preferredDataType != null)
				preferredDataType.PreferredDataType = DataType;

			return (T)result;
		}

		#region IFactory Members
			Type IFactory.FactoryType
			{
				get
				{
					return typeof(T);
				}
			}

			object IFactory.Create()
			{
				return Create();
			}
		#endregion
	}
}
