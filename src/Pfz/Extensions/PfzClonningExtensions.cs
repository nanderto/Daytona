using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Pfz.Serialization;

namespace Pfz.Extensions
{
	/// <summary>
	/// Adds some methods for clonning objects.
	/// </summary>
	public static class PfzClonningExtensions
	{
		/// <summary>
		/// Clones the source object returning the new one as the same type
		/// (cast) of the original.
		/// </summary>
		public static T TypedClone<T>(this T source)
		where
			T: ICloneable
		{
			if (source == null)
				throw new ArgumentNullException("source");
				
			return (T)source.Clone();
		}

		/// <summary>
		/// Does a clone of an ICloneable&lt;T&gt;, checking if the source is null.
		/// If it is, returns null, instead of throwing an exception.
		/// </summary>
		public static T CheckedClone<T>(this T source)
		where
			T: ICloneable<T>
		{
			if (source == null)
				return source;

			return source.Clone();
		}
	}

	namespace ClonningExtensions
	{
		/// <summary>
		/// Class that contains CloneBySerialization method.
		/// As the name says, it adds a method that allows to deep-clone objects, using a process similar
		/// to serialization/deserialization.
		/// </summary>
		public static class PfzCloneBySerializationExtension
		{
			/// <summary>
			/// Clones an object by using a process similar to serialization/deserialization.
			/// </summary>
			public static T CloneBySerialization<T>(this T source)
			{
				if (source == null)
					throw new ArgumentNullException("source");
			
				List<IDeserializationCallback> callbacks = new List<IDeserializationCallback>();
				Dictionary<object, object> clonnedObjects = new Dictionary<object, object>(ReferenceComparer.Instance);
				object result = _Clone(callbacks, clonnedObjects, source);
			
				StreamingContext context = new StreamingContext(StreamingContextStates.Clone);
			
				foreach(var callback in callbacks)
					callback.OnDeserialization(context);
			
				return (T)result;
			}
			private static object _Clone(List<IDeserializationCallback> callbacks, Dictionary<object, object> clonnedObjects, object graph)
			{
				if (graph == null)
					return null;
			
				// strings are read-only, so no clonning is needed.
				if (graph is string)
					return graph;
				
				// optimization for DBNull
				if (graph == DBNull.Value)
					return DBNull.Value;
			
				// optimization for MemberInfos in general.
				if (graph is MemberInfo)
					return graph;
			
				StreamingContext context = new StreamingContext(StreamingContextStates.Clone);
				BinarySerializer._InvokeOnSerializing(graph, context);
				object result = _Clone2(callbacks, clonnedObjects, graph);
				BinarySerializer._InvokeOnSerialized(graph, context);
				BinarySerializer._InvokeOnDeserialized(result, context);
				return result;
			}
			private static object _Clone2(List<IDeserializationCallback> callbacks, Dictionary<object, object> clonnedObjects, object graph)
			{
				Type type = graph.GetType();
			
				if (!type.IsSerializable)
					throw new ArgumentException("graph of type " + type.FullName + " is not serializable.");
			
				object result = null;
				if (!type.IsValueType)
					if (clonnedObjects.TryGetValue(graph, out result))
						return result;
			
			
				if (type.IsArray)
				{
					Array sourceArray = (Array)graph;
					Array resultArray = (Array)sourceArray.Clone();
				
					_AddClonnedObject(graph, resultArray, callbacks, clonnedObjects);
				
					Type elementType = type.GetElementType();
					if (!_HasAnyReference(elementType))
						return resultArray;
					
					int dimensions = resultArray.Rank;
					int[] indices = new int[dimensions];
					for (int i=0; i<dimensions; i++)
						indices[i] = resultArray.GetLowerBound(i);
				
					int lastDimension = dimensions-1;
					indices[lastDimension]--;

					while(true)
					{
						int actualDimension = lastDimension;
						while(true)
						{
							int index = indices[actualDimension] + 1;
							if (index <= resultArray.GetUpperBound(actualDimension))
							{
								indices[actualDimension] = index;
								object item = _Clone(callbacks, clonnedObjects, resultArray.GetValue(indices));
								resultArray.SetValue(item, indices);
								break;
							}

							if (actualDimension == 0)
								return resultArray;
							
							indices[actualDimension] = resultArray.GetLowerBound(actualDimension);
							actualDimension--;
						}
					}
				}

				ISerializable serializable = graph as ISerializable;
				if (serializable != null)
				{
					SerializationInfo info = new SerializationInfo(type, new FormatterConverter());
					StreamingContext context = new StreamingContext(StreamingContextStates.Clone);
					serializable.GetObjectData(info, context);
				
					ConstructorInfo constructorInfo = null;
				
					if (info.AssemblyName == type.Assembly.FullName && info.FullTypeName == type.FullName)
					{
						result = BinarySerializer._FormatterServicesGetSafeUninitializedObject(type, new StreamingContext(StreamingContextStates.Clone));
					
						_AddClonnedObject(graph, result, callbacks, clonnedObjects);

						constructorInfo = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, BinarySerializer._deserializationConstructorTypes, null);
					}
					else
					{
						Type realType = Assembly.Load(info.AssemblyName).GetType(info.FullTypeName, true);
						constructorInfo = realType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, BinarySerializer._deserializationConstructorTypes, null);
					}

					if (constructorInfo == null)
						throw new SerializationException("Couldn't find Deserialization constructor for type " + type.FullName + ".");
					
					var parameters = new object[]{info, context};
				
					if (result != null)
						constructorInfo.Invoke(result, parameters);
					else
					{
						result = constructorInfo.Invoke(parameters);

						IObjectReference objectReference = result as IObjectReference;
						if (objectReference != null)
						{
							result = objectReference.GetRealObject(context);
						
							IDeserializationCallback callback = objectReference as IDeserializationCallback;
							if (callback != null)
								callbacks.Add(callback);
						}

						_AddClonnedObject(graph, result, callbacks, clonnedObjects);
					}

					return result;
				}
			
				result = BinarySerializer._FormatterServicesGetSafeUninitializedObject(type, new StreamingContext(StreamingContextStates.Clone));
			
				_AddClonnedObject(graph, result, callbacks, clonnedObjects);

				var fields = BinarySerializer._GetFields(type);
				object[] values = FormatterServices.GetObjectData(graph, fields);
				int length = values.Length;
				for (int i=0; i<length; i++)
				{
					object value = values[i];
					if (value != null && _HasAnyReference(value.GetType()))
					{
						value = _Clone(callbacks, clonnedObjects, value);
						values[i] = value;
					}
				}
				FormatterServices.PopulateObjectMembers(result, fields, values);
				return result;
			}

			private static void _AddClonnedObject(object graph, object result, List<IDeserializationCallback> callbacks, Dictionary<object, object> clonnedObjects)
			{
				if (!graph.GetType().IsValueType)
					clonnedObjects.Add(graph, result);
			
				IDeserializationCallback callback = result as IDeserializationCallback;
				if (callback != null)
					callbacks.Add(callback);
			}

			private static Dictionary<Type, bool> _hasAnyReferenceDictionary = new Dictionary<Type, bool>();
			private static bool _HasAnyReference(Type type)
			{
				if (type.IsPrimitive)
					return false;
			
				if (!type.IsValueType)
					return true;
				
				if (type == typeof(string))
					return false;
			
				if (typeof(ISerializable).IsAssignableFrom(type))
					return true;
			
				lock(_hasAnyReferenceDictionary)
				{
					bool result;
				
					if (!_hasAnyReferenceDictionary.TryGetValue(type, out result))
					{
						FieldInfo[] fields = BinarySerializer._GetFields(type);
						foreach(var field in fields)
						{
							if (_HasAnyReference(field.FieldType))
							{
								result = true;
								break;
							}
						}
					
						_hasAnyReferenceDictionary.Add(type, result);
					}
				
					return result;
				}
			}
		}
	}
}
