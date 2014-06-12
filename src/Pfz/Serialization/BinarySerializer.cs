﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using Pfz.DataTypes;
using Pfz.Extensions;

namespace Pfz.Serialization
{
	/// <summary>
	/// A binary serializer faster and more compressed than custom .Net 
	/// BinaryFormatter.
	/// It DOES NOT take the many serialization attributes 
	/// and other serialization objects into account.
	/// </summary>
	public sealed class BinarySerializer:
		IFormatter
	{
		#region Private Fields
			private int _idGenerator;
			private int _assemblyGenerator;
			private int _typeGenerator;
			private Stream _stream;
		#endregion
		
		/// <summary>
		/// Gets or sets the Context used by the ISerializable interface.
		/// </summary>
		public StreamingContext Context { get; set; }
		
		#region DefaultTypes - Methods
			private readonly HashSet<Assembly> _defaultAssemblies = new HashSet<Assembly>();
			private readonly HashSet<Type> _defaultTypes = new HashSet<Type>();
			
			/// <summary>
			/// This method adds a type to the "automatic type list".
			/// This avoids such type to be saved in serialized streams, but the
			/// deserializer must add the exactly same types, in the exactly same
			/// order.
			/// Returns a boolean value indicating if such type was added (true),
			/// or if was already added before (false).
			/// </summary>
			public bool AddDefaultType(Type type)
			{
				if (type == null)
					throw new ArgumentNullException("type");
					
				if (!type.IsSerializable)
					throw new ArgumentException("type must be serializable.\r\nType: " + type.FullName, "type");
					
				if (type.IsAbstract && type != typeof(Type))
					throw new ArgumentException("type must not be abstract.\r\nType: " + type.FullName, "type");
					
				_defaultAssemblies.Add(type.Assembly);
				return _defaultTypes.Add(type);
			}
			
			/// <summary>
			/// Adds primitive types as Default-Types.
			/// </summary>
			public void AddPrimitivesAsDefault()
			{
				_defaultAssemblies.Add(typeof(int).Assembly);
				_defaultTypes.Add(typeof(int));
				_defaultTypes.Add(typeof(long));
				_defaultTypes.Add(typeof(byte));
				_defaultTypes.Add(typeof(short));
				_defaultTypes.Add(typeof(uint));
				_defaultTypes.Add(typeof(ulong));
				_defaultTypes.Add(typeof(sbyte));
				_defaultTypes.Add(typeof(ushort));
				_defaultTypes.Add(typeof(bool));
				_defaultTypes.Add(typeof(char));
				_defaultTypes.Add(typeof(float));
				_defaultTypes.Add(typeof(double));
			}
			
			/// <summary>
			/// Adds primitives, string, DateTime, decimal and some other common
			/// types as default types.
			/// </summary>
			public void AddRecommendedDefaults()
			{
				AddPrimitivesAsDefault();
				_defaultAssemblies.Add(typeof(Date).Assembly);
				_defaultTypes.Add(typeof(string));
				_defaultTypes.Add(typeof(decimal));
				_defaultTypes.Add(typeof(Date));
				_defaultTypes.Add(typeof(DateTime));
				_defaultTypes.Add(typeof(Time));
				_defaultTypes.Add(typeof(Type));
				_defaultTypes.Add(typeof(byte[]));
				_defaultTypes.Add(typeof(int[]));
			}
			
			/// <summary>
			/// Adds the given type to the list of default types, and every
			/// type referenced by this type, directly or indirectly, that is
			/// not a value type (as the Type for value type references are 
			/// never serialized).
			/// </summary>
			public void AddDefaultTypeRecursive(Type type)
			{
				if (type == null)
					throw new ArgumentNullException("type");

				if (!type.IsAbstract || type == typeof(Type))
					AddDefaultType(type);

				HashSet<Type> abstractTypes = new HashSet<Type>();
				_AddDefaultTypeRecursive(type, abstractTypes);
			}
			private void _AddDefaultTypeRecursive(Type type, HashSet<Type> abstractTypes)
			{
				if (type == typeof(Type))
					return;

				if (typeof(ISerializable).IsAssignableFrom(type))
					return;
					
				var fields = _GetFields(type);
				foreach(var field in fields)
				{
					Type fieldType = field.FieldType;
					
					if ((fieldType.IsAbstract && fieldType != typeof(Type)) || (fieldType.IsValueType && (!fieldType.IsGenericType || fieldType.GetGenericTypeDefinition() != typeof(Nullable<>))))
					{
						if (!abstractTypes.Add(fieldType))
							continue;
					}
					else
					{
						if (!AddDefaultType(fieldType))
							continue;
					}
						
					_AddDefaultTypeRecursive(fieldType, abstractTypes);
				}
			}
			
			/// <summary>
			/// Add the nullable version of already added value-type defaults
			/// as default values.
			/// </summary>
			public void AddNullableOfDefaultsAsDefaults()
			{
				bool wasAdded = false;
				var array = _defaultTypes.ToArray();
				foreach(var item in array)
				{
					if (!item.IsValueType)
						continue;
					
					if (item.IsGenericType && item.GetGenericTypeDefinition() == typeof(Nullable<>))
						continue;
					
					var nullableType = typeof(Nullable<>).MakeGenericType(item);
					_defaultTypes.Add(nullableType);
					wasAdded = true;
				}
				
				if (wasAdded)
					_defaultAssemblies.Add(typeof(Nullable<>).Assembly);
			}
		#endregion
		#region InvokeAll - Private Methods
			private static readonly Dictionary<Type, MethodInfo[]> _onSerializings = new Dictionary<Type, MethodInfo[]>();
			internal static void _InvokeOnSerializing(object graph, StreamingContext context)
			{
				_InvokeAllMethodsWith(_onSerializings, graph, typeof(OnSerializingAttribute), context);
			}

			private static readonly Dictionary<Type, MethodInfo[]> _onSerializeds = new Dictionary<Type, MethodInfo[]>();
			internal static void _InvokeOnSerialized(object graph, StreamingContext context)
			{
				_InvokeAllMethodsWith(_onSerializeds, graph, typeof(OnSerializedAttribute), context);
			}
			
			private static readonly Dictionary<Type, MethodInfo[]> _onDeserializings = new Dictionary<Type, MethodInfo[]>();
			internal static void _InvokeOnDeserializing(object graph, StreamingContext context)
			{
				_InvokeAllMethodsWith(_onDeserializings, graph, typeof(OnDeserializingAttribute), context);
			}

			private static readonly Dictionary<Type, MethodInfo[]> _onDeserializeds = new Dictionary<Type, MethodInfo[]>();
			internal static void _InvokeOnDeserialized(object graph, StreamingContext context)
			{
				_InvokeAllMethodsWith(_onDeserializeds, graph, typeof(OnDeserializedAttribute), context);
			}

			private static void _InvokeAllMethodsWith(Dictionary<Type, MethodInfo[]> dictionary, object graph, Type attributeType, StreamingContext context)
			{
				MethodInfo[] result;
				
				Type type = graph.GetType();
				lock(dictionary)
				{
					if (!dictionary.TryGetValue(type, out result))
					{
						List<MethodInfo> list = new List<MethodInfo>();
						Type actualType = type;
						while(actualType != null)
						{
							MethodInfo[] methods = actualType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
							foreach(var method in methods)
								if (method.GetCustomAttributes(attributeType, false).Length > 0)
									list.Add(method);
						
							actualType = actualType.BaseType;
						}
						
						if (list.Count > 0)
							result = list.ToArray();
							
						dictionary.Add(type, result);
					}
				}
				
				if (result != null)
				{
					var parameters = new object[]{context};
					foreach(var method in result)
						method.Invoke(graph, parameters);
				}
			}
		#endregion
		#region Serialization Methods
			private FormatterConverter _formatterConverter = new FormatterConverter();
			private Dictionary<Assembly, int> _allAssemblies;
			private Dictionary<Type, int> _allTypes;
			private Dictionary<object, int> _allValues;
			private static Dictionary<Type, FieldInfo[]> _fieldInfos = new Dictionary<Type, FieldInfo[]>();
			private Dictionary<object, SerializationInfo> _serializationInfos;
			private Dictionary<object, Type> _itemsToReplace;
			
			/// <summary>
			/// Serializes the given object.
			/// </summary>
			public void Serialize(Stream stream, object graph)
			{
				if (stream == null)
					throw new ArgumentNullException("stream");
				
				if (graph == null)
					throw new ArgumentNullException("graph");
				
				_idGenerator = 0;
				_assemblyGenerator = _defaultAssemblies.Count;
				_typeGenerator = _defaultTypes.Count;
				try
				{
					_allAssemblies = new Dictionary<Assembly, int>();
					int countDefaultAssemblies = _defaultAssemblies.Count;
					int assemblyIndex = -1;
					foreach(Assembly assembly in _defaultAssemblies)
					{
						assemblyIndex ++;
						_allAssemblies.Add(assembly, assemblyIndex);
					}
					
					_allTypes = new Dictionary<Type, int>(64);
					int countDefaultTypes = _defaultTypes.Count;
					int typeIndex = -1;
					foreach(Type type in _defaultTypes)
					{
						typeIndex++;
						_allTypes.Add(type, typeIndex);
					}
					
					_allValues = new Dictionary<object, int>(ReferenceComparer.Instance);
					_itemsToReplace = new Dictionary<object, Type>(ReferenceComparer.Instance);
					_stream = stream;
					
					_InitializeAllValues(typeof(object), graph);
					
					foreach(var fakeType in _itemsToReplace.Values)
						_AddType(fakeType);
					
					int countAssemblies = _allAssemblies.Count;
					_WriteCompressedInt(countAssemblies - countDefaultAssemblies);
					foreach(var assembly in _allAssemblies.Keys.Skip(countDefaultAssemblies))
						_WriteString(assembly.FullName);
					
					int countTypes = _allTypes.Count;
					_WriteCompressedInt(countTypes - countDefaultTypes);
					foreach(var type in _allTypes.Keys.Skip(countDefaultTypes))
					{
						_WriteCompressedInt(_allAssemblies[type.Assembly]);
						_WriteString(type.FullName);
					}
					
					_allAssemblies = null;
					
					_WriteCompressedInt(_allValues.Count);
					foreach(var itemReal in _allValues.Keys)
					{
						object item = itemReal;
						Type type = item.GetType();

						Type fakeType;
						if (_itemsToReplace.TryGetValue(item, out fakeType))
						{
							type = fakeType;
							item = FormatterServices.GetSafeUninitializedObject(type);
						}

						_WriteCompressedInt(_allTypes[type]);
						
						Array array = item as Array;
						if (array != null)
						{
							int dimensions = array.Rank;
							_WriteCompressedInt(dimensions);
							
							for (int i=0; i<dimensions; i++)
							{
								_WriteCompressedInt(array.GetLength(i));
								_WriteCompressedInt(array.GetLowerBound(i));
							}
						}
						else
						{
							string str = item as string;
							if (str != null)
								_WriteString(str);
						}
					}
					
					_idGenerator = 0;
					_allValues.Clear();
					_Serialize(typeof(object), graph);
				}
				finally
				{
					_allValues = null;
					_allAssemblies = null;
					_allTypes = null;
					_stream = null;
					_serializationInfos = null;
					_itemsToReplace = null;
				}
			}

			private void _AddType(Type type)
			{
				if (!_allTypes.ContainsKey(type))
				{
					_allTypes.Add(type, _typeGenerator++);
					
					Assembly assembly = type.Assembly;
					if (!_allAssemblies.ContainsKey(assembly))
						_allAssemblies.Add(assembly, _assemblyGenerator++);
				}
			}

			[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
			private void _InitializeAllValues(Type expectedType, object graph)
			{
				if (graph == null || graph == DBNull.Value)
					return;
			
				Type realType = graph.GetType();
				if (!realType.IsSerializable)
					throw new ArgumentException("graph is not serializable.", "graph");
				
				int id;
				if (_allValues.TryGetValue(graph, out id))
					return;
			
				Type typeGraph = graph as Type;
				if (typeGraph != null)
				{
					Assembly assembly = typeGraph.Assembly;
					int assemblyIndex;
					if (!_allAssemblies.TryGetValue(assembly, out assemblyIndex))
					{
						assemblyIndex = _assemblyGenerator++;
						_allAssemblies.Add(assembly, assemblyIndex);
					}
							
					int typeIndex;
					if (!_allTypes.TryGetValue(typeGraph, out typeIndex))
					{
						typeIndex = _typeGenerator++;
						_allTypes.Add(typeGraph, typeIndex);
					}
					return;
				}

				_InvokeOnSerializing(graph, Context);
			
				if (!expectedType.IsValueType || expectedType.IsGenericType && expectedType.GetGenericTypeDefinition() == typeof(Nullable<>))
				{
					_AddType(realType);
				
					if (!realType.IsValueType)
						_allValues.Add(graph, _idGenerator++);
				}

				ISerializable serializable = graph as ISerializable;
				if (serializable != null)
				{
					SerializationInfo info = new SerializationInfo(realType, _formatterConverter);
					serializable.GetObjectData(info, Context);
					
					if (_serializationInfos == null)
						_serializationInfos = new Dictionary<object, SerializationInfo>();
						
					_serializationInfos[graph] = info;
					foreach(var item in info)
					{
						object value = item.Value;
						_InitializeAllValues(typeof(object), value);
					}
					
					if (info.FullTypeName != realType.FullName || info.AssemblyName != realType.Assembly.FullName)
					{
						Assembly assembly = Assembly.Load(info.AssemblyName);
						Type fakeType = assembly.GetType(info.FullTypeName, true);
						_itemsToReplace[graph] = fakeType;
					}
					
					return;
				}
				
				if (serializable != null)
					return;
					
				if (realType.Assembly == typeof(int).Assembly)
				{
					switch(realType.Name)
					{
						case "Boolean[]":
						case "Byte[]":
						case "Char[]":
						case "Int64[]":
						case "Int32[]":
						case "Int16[]":
						case "UInt64[]":
						case "UInt32[]":
						case "UInt16[]":
						case "SByte[]":
						case "String":
						case "Int32":
						case "Int64":
						case "Int16":
						case "Byte":
						case "UInt32":
						case "UInt64":
						case "UInt16":
						case "SByte":
						case "Single":
						case "Double":
						case "Char":
						case "Boolean":
							return;
					}
					
					if (expectedType == typeof(bool?) || realType == typeof(bool?[]))
						return;
				}

				if (realType.IsArray)
				{
					Array array = (Array)graph;
					Type elementType = realType.GetElementType();
					if (!elementType.IsPrimitive)
					{
						int length = array.Length;
						foreach(var obj in array)
							_InitializeAllValues(elementType, obj);
					}

					return;
				}
				
				if (realType.IsPrimitive)
					throw new NotImplementedException("Need to support more primitives. Failed for: " + realType.FullName + ".");	

				var fields = _GetFields(realType);
				var values = FormatterServices.GetObjectData(graph, fields);
				
				int count = fields.Length;
				for(int i=0; i<count; i++)
				{
					object fieldValue = values[i];
					if (fieldValue == null)
						continue;
					
					FieldInfo field = fields[i];
					_InitializeAllValues(field.FieldType, fieldValue);
				}
			}
			[SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily"), SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
			private void _Serialize(Type expectedType, object graph)
			{
				if (graph == null)
				{
					_stream.WriteByte(3);
					return;
				}
				if (graph == DBNull.Value)
				{
					_stream.WriteByte(4);
					return;
				}
				
				Type realType = graph.GetType();
				if (!realType.IsSerializable)
					throw new ArgumentException("graph is not serializable.", "graph");
			
				int id;
				if (_allValues.TryGetValue(graph, out id))
				{
					_WriteCompressedInt(id + 7);
					return;
				}

				Type typeGraph = graph as Type;
				if (typeGraph != null)
				{
					int typeIndex = _allTypes[typeGraph];
					_WriteCompressedInt(6);
					_WriteCompressedInt(typeIndex);
					return;
				}

				if (!expectedType.IsValueType || expectedType.IsGenericType && expectedType.GetGenericTypeDefinition() == typeof(Nullable<>))
				{
					if (realType.IsValueType)
					{
						_stream.WriteByte(5);
						_WriteCompressedInt(_allTypes[realType]);
					}
					else
					{
						id = _idGenerator++;
						_allValues.Add(graph, id);
						
						if (!(graph is string))
							_stream.WriteByte(0);
					}
				}

				ISerializable serializable = graph as ISerializable;
				if (serializable != null)
				{
					var info = _serializationInfos[graph];
					
					_WriteCompressedInt(info.MemberCount);
					foreach(var item in info)
					{
						_WriteString(item.Name);
						
						object value = item.Value;
						_Serialize(typeof(object), value);
					}
					
					_InvokeOnSerialized(graph, Context);
					return;
				}

				if (realType.Assembly == typeof(int).Assembly)
				{
					if (expectedType == typeof(bool?))
					{
						bool value = (bool)graph;
						if (value)
							_stream.WriteByte(1);
						else
							_stream.WriteByte(0);
						
						return;
					}
					
					switch(realType.Name)
					{
						case "Boolean[]":
							_WriteBooleanArray((bool[])graph);
							return;
							
						case "Byte[]":
							byte[] byteArray = (byte[])graph;
							_stream.Write(byteArray);
							return;
						
						case "Char[]":
							_WriteString(new string((char[])graph));
							return;
							
						case "Int64[]":
						case "Int32[]":
						case "Int16[]":
						case "UInt64[]":
						case "UInt32[]":
						case "UInt16[]":
						case "SByte[]":
							Type elementType = realType.GetElementType();
							Array array = (Array)graph;
							int length = Buffer.ByteLength(array);
							byteArray = new byte[length];
							Buffer.BlockCopy(array, 0, byteArray, 0, length);
							_stream.Write(byteArray, 0, length);
							return;
						
						case "String":
							_stream.WriteByte(2); // reference string, generating new id.
							return;
						
						case "Int32":
							_stream.Write(BitConverter.GetBytes((int)graph), 0, 4);
							return;
						
						case "Int64":
							_stream.Write(BitConverter.GetBytes((long)graph), 0, 8);
							return;
						
						case "Int16":
							_stream.Write(BitConverter.GetBytes((short)graph), 0, 2);
							return;
						
						case "Byte":
							_stream.WriteByte((byte)graph);
							return;
						
						case "UInt32":
							_stream.Write(BitConverter.GetBytes((uint)graph), 0, 4);
							return;
						
						case "UInt64":
							_stream.Write(BitConverter.GetBytes((ulong)graph), 0, 8);
							return;
						
						case "UInt16":
							_stream.Write(BitConverter.GetBytes((ushort)graph), 0, 2);
							return;
						
						case "SByte":
							_stream.WriteByte((byte)((sbyte)graph));
							return;
							
						case "Single":
							_stream.Write(BitConverter.GetBytes((float)graph), 0, 4);
							return;
							
						case "Double":
							_stream.Write(BitConverter.GetBytes((double)graph), 0, 8);
							return;
						
						case "Char":
							_stream.Write(BitConverter.GetBytes((char)graph), 0, 2);
							return;
						
						case "Boolean":
							_stream.WriteByte((bool)graph ? (byte)1 : (byte)0);
							return;
					}
					
					if (realType == typeof(bool?[]))
					{
						bool?[] array = (bool?[])graph;
						_WriteNullableBooleanArray(array);
						return;
					}
				}
				
				if (realType.IsArray)
				{
					Array array = (Array)graph as Array;
			
					Type elementType = realType.GetElementType();
					foreach(var obj in array)
						_Serialize(elementType, obj);
					
					return;
				}

				if (realType.IsPrimitive)
					throw new NotImplementedException("Need to support more primitives. Failed for: " + realType.FullName + ".");	
					
				var fields = _GetFields(realType);
				var values = FormatterServices.GetObjectData(graph, fields);
				
				int count = fields.Length;
				for(int i=0; i<count; i++)
				{
					object fieldValue = values[i];
					FieldInfo field = fields[i];
					_Serialize(field.FieldType, fieldValue);
				}

				_InvokeOnSerialized(graph, Context);
			}
			internal static FieldInfo[] _GetFields(Type type)
			{
				FieldInfo[] result;
				
				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
					type = type.GetGenericArguments()[0];
				
				lock(_fieldInfos)
				{
					if (_fieldInfos.TryGetValue(type, out result))
						return result;
					
					List<FieldInfo> list = new List<FieldInfo>();
					Type actualType = type;
					while(actualType != null)
					{
						result = actualType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
						
						foreach(var field in result)
							if (!field.ContainsCustomAttribute<NonSerializedAttribute>() && !field.ContainsCustomAttribute<OptionalFieldAttribute>())
								list.Add(field);
						
						actualType = actualType.BaseType;
					}
					
					result = list.ToArray();
					_fieldInfos.Add(type, result);
				}

				return result;
			}
			
			private void _WriteString(string value)
			{
				byte[] buffer = Encoding.UTF8.GetBytes(value);
				_WriteByteArray(buffer);
			}
			private void _WriteByteArray(byte[] byteArray)
			{
				_WriteCompressedInt(byteArray.Length);
				_stream.Write(byteArray, 0, byteArray.Length);
			}
		#endregion
		#region Deserialization Methods
			private byte[] _intBytes;
			internal static readonly Type[] _deserializationConstructorTypes = new Type[]{typeof(SerializationInfo), typeof(StreamingContext)};
			private object[] _referencedObjects;
			private List<IDeserializationCallback> _callbacks;
			private Type[] _types;
			
			/// <summary>
			/// Deserializes an object from the given stream.
			/// </summary>
			public object Deserialize(Stream stream)
			{
				if (stream == null)
					throw new ArgumentNullException("stream");
					
				_idGenerator = 0;
				
				if (_intBytes == null)
					_intBytes = new byte[8];
					
				try
				{
					_callbacks = new List<IDeserializationCallback>();
				
					_stream = stream;
					
					int countDefaultAssemblies = _defaultAssemblies.Count;
					int countAssemblies = _ReadCompressedInt();
					Assembly[] assemblies = new Assembly[countDefaultAssemblies + countAssemblies];
					_defaultAssemblies.CopyTo(assemblies, 0);
					for(int i=0; i<countAssemblies; i++)
					{
						string assemblyName = _ReadString();
						Assembly assembly = AppDomain.CurrentDomain.Load(assemblyName);
						assemblies[countDefaultAssemblies + i] = assembly;
					}
					
					int countDefaultTypes = _defaultTypes.Count;
					int countTypes = _ReadCompressedInt();
					Type[] types = new Type[countDefaultTypes + countTypes];
					_types = types;
					_defaultTypes.CopyTo(types, 0);
					for(int i=0; i<countTypes; i++)
					{
						int assemblyIndex = _ReadCompressedInt();
						string typeName = _ReadString();
						
						Assembly assembly = assemblies[assemblyIndex];
						Type type = assembly.GetType(typeName, true);
						types[i + countDefaultTypes] = type;
					}
					
					int count = _ReadCompressedInt();
					_referencedObjects = new object[count];
					for (int i=0; i<count; i++)
					{
						int typeIndex = _ReadCompressedInt();
						Type type = types[typeIndex];
						
						object instance;
						if (type.IsArray)
						{
							int countDimensions = _ReadCompressedInt();
							int[] lengths = new int[countDimensions];
							int[] lowerBounds = new int[countDimensions];
							for (int dimension=0; dimension<countDimensions; dimension++)
							{
								lengths[dimension] = _ReadCompressedInt();
								lowerBounds[dimension] = _ReadCompressedInt();
							}
							
							instance = Array.CreateInstance(type.GetElementType(), lengths, lowerBounds);
						}
						else
						if (type == typeof(string))
							instance = _ReadString();
						else
							instance = _FormatterServicesGetSafeUninitializedObject(type, Context);
							
						_referencedObjects[i] = instance;
					}
					
					object result = _Deserialize(typeof(object));
					
					int countCallbacks = _callbacks.Count;
					for (int i=countCallbacks-1; i>=0; i--)
					{
						IDeserializationCallback callback = _callbacks[i];
						callback.OnDeserialization(Context);
					}
					
					return result;
				}
				finally
				{
					_types = null;
					_stream = null;
					_referencedObjects = null;
					_callbacks = null;
				}
			}

			internal static object _FormatterServicesGetSafeUninitializedObject(Type type, StreamingContext context)
			{
				object result = FormatterServices.GetSafeUninitializedObject(type);
				_InvokeOnDeserializing(result, context);
				return result;
			}

			[SuppressMessage("Microsoft.Maintainability", "CA1505:AvoidUnmaintainableCode"), SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
			private object _Deserialize(Type expectedType)
			{
				if (expectedType == typeof(bool?))
				{
					switch(_ReadByte())
					{
						case 0:
							return false;
						
						case 1:
							return true;
						
						case 3:
							return null;
					}

					throw new SerializationException("Invalid byte in stream.");
				}
				
				Type realType = expectedType;
				bool isValueType = expectedType.IsValueType;
				if (!isValueType || expectedType.IsGenericType && expectedType.GetGenericTypeDefinition() == typeof(Nullable<>))
				{
					int nextCommand = _ReadCompressedInt();
					switch(nextCommand)
					{
						case 6:
							return _types[_ReadCompressedInt()];

						case 5:
							isValueType = true;
							int typeId = _ReadCompressedInt();
							realType = _types[typeId];
							break;
							
						case 4:
							return DBNull.Value;
							
						case 3:
							return null;
						
						case 2:
							return _referencedObjects[_idGenerator++];
							
						case 0:
							break;
						
						default:
							int id = nextCommand - 7;
							
							return _referencedObjects[id];
					}
				}

				object result = null;
				
				if (expectedType.Assembly == typeof(Nullable<>).Assembly)
					if (expectedType.IsGenericType && expectedType.GetGenericTypeDefinition() == typeof(Nullable<>))
						expectedType = expectedType.GetGenericArguments()[0];
						
				int actualId = -1;
				if (!isValueType)
				{
					actualId = _idGenerator++;
					result = _referencedObjects[actualId];
					realType = result.GetType();
				}
				
				if (typeof(ISerializable).IsAssignableFrom(realType))
				{
					SerializationInfo info = new SerializationInfo(expectedType, _formatterConverter);
					int count = _ReadCompressedInt();
					for(int i=0; i<count; i++)
					{
						string name = _ReadString();
						object value = _Deserialize(typeof(object));
						
						info.AddValue(name, value);
					}
					
					ConstructorInfo constructorInfo = realType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, _deserializationConstructorTypes, null);
					if (constructorInfo == null)
						throw new SerializationException("Couldn't find Deserialization constructor for type " + realType.FullName + ".");
					
					var parameters = new object[]{info, Context};
					if (result == null)
						result = constructorInfo.Invoke(parameters);
					else
						constructorInfo.Invoke(result, parameters);
						
					IDeserializationCallback callback = result as IDeserializationCallback;
					if (callback != null)
						_callbacks.Add(callback);

					IObjectReference reference = result as IObjectReference;
					if (reference != null)
					{
						result = reference.GetRealObject(Context);
						
						if (actualId != -1)
							_referencedObjects[actualId] = result;

						callback = result as IDeserializationCallback;
						if (callback != null)
							_callbacks.Add(callback);
					}
					
					_InvokeOnDeserialized(result, Context);
					return result;
				}

				if (realType.Assembly == typeof(int).Assembly)
				{
					switch(realType.Name)
					{
						case "Boolean[]":
							_ReadBooleanArray((bool[])result);
							return result;
							
						case "Byte[]":
							_stream.FullRead((byte[])result);
							return result;
							
						case "Char[]":
							string str = _ReadString();
							str.CopyTo(0, (char[])result, 0, str.Length);
							return result;

						case "Int64[]":
						case "Int32[]":
						case "Int16[]":
						case "UInt64[]":
						case "UInt32[]":
						case "UInt16[]":
						case "SByte[]":
							Type elementType = realType.GetElementType();
							Array array = (Array)result;
							int count = array.Length;
							int byteLength = count * Marshal.SizeOf(elementType);
							byte[] byteArray = new byte[byteLength];
							_stream.FullRead(byteArray);
							Buffer.BlockCopy(byteArray, 0, array, 0, byteLength);
							return result;

						case "String":
							throw new SerializationException("Impossible condition in stream.");
							
						case "Int32":
						{
							_stream.FullRead(_intBytes, 0, 4);
							result = BitConverter.ToInt32(_intBytes, 0);
							return result;
						}
						
						case "Int64":
						{
							_stream.FullRead(_intBytes, 0, 8);
							result = BitConverter.ToInt64(_intBytes, 0);
							return result;
						}
						
						case "Int16":
						{
							_stream.FullRead(_intBytes, 0, 2);
							result = BitConverter.ToInt16(_intBytes, 0);
							return result;
						}
						
						case "UInt32":
						{
							_stream.FullRead(_intBytes, 0, 4);
							result = BitConverter.ToUInt32(_intBytes, 0);
							return result;
						}
						
						case "UInt64":
						{
							_stream.FullRead(_intBytes, 0, 8);
							result = BitConverter.ToUInt64(_intBytes, 0);
							return result;
						}
						
						case "UInt16":
						{
							_stream.FullRead(_intBytes, 0, 2);
							result = BitConverter.ToUInt16(_intBytes, 0);
							return result;
						}

						case "Byte":
							result = _ReadByte();
							return result;
						
						case "SByte":
							result = (sbyte)_ReadByte();
							return result;
						
						case "Single":
						{
							_stream.FullRead(_intBytes, 0, 4);
							result = BitConverter.ToSingle(_intBytes, 0);
							return result;
						}
						
						case "Double":
						{
							_stream.FullRead(_intBytes, 0, 8);
							result = BitConverter.ToDouble(_intBytes, 0);
							return result;
						}
						
						case "Char":
						{
							_stream.FullRead(_intBytes, 0, 2);
							result = BitConverter.ToChar(_intBytes, 0);
							return result;
						}
						
						case "Boolean":
							result = _ReadByte() != 0;
							return result;

						case "Type":
							int typeIndex = _ReadCompressedInt();
							result = _types[typeIndex];
							return result;
					}

					if (realType == typeof(bool?[]))
					{
						_ReadNullableBooleanArray((bool?[])result);
						return result;
					}
				}

				if (realType.IsArray)
				{
					Type elementType = realType.GetElementType();
					Array array = (Array)result;
					
					int dimensions = array.Rank;
					int[] indices = new int[dimensions];
					for (int i=0; i<dimensions; i++)
						indices[i] = array.GetLowerBound(i);
					
					int lastDimension = dimensions-1;
					indices[lastDimension]--;

					while(true)
					{
						int actualDimension = lastDimension;
						while(true)
						{
							int index = indices[actualDimension] + 1;
							if (index <= array.GetUpperBound(actualDimension))
							{
								indices[actualDimension] = index;
								object item = _Deserialize(elementType);
								array.SetValue(item, indices);
								break;
							}

							if (actualDimension == 0)
								return array;
								
							indices[actualDimension] = array.GetLowerBound(actualDimension);
							actualDimension--;
						}
					}
				}

				if (realType.IsPrimitive)
					throw new NotImplementedException("Must support more primitives.");

				{
					var fields = _GetFields(realType);
					int count = fields.Length;
					object[] array = new object[count];
					for(int i=0; i<count; i++)
					{
						FieldInfo field = fields[i];
						object item = _Deserialize(field.FieldType);
						array[i] = item;
					}
					
					if (result == null)
						result = _FormatterServicesGetSafeUninitializedObject(realType, Context);
						
					IDeserializationCallback callback = result as IDeserializationCallback;
					if (callback != null)
						_callbacks.Add(callback);
						
					FormatterServices.PopulateObjectMembers(result, fields, array);
					_InvokeOnDeserialized(result, Context);
					
					return result;
				}
			}

			private string _ReadString()
			{
				byte[] bytes = _ReadByteArray();
				return Encoding.UTF8.GetString(bytes);
			}
			private byte[] _ReadByteArray()
			{
				int length = _ReadCompressedInt();
				byte[] result = new byte[length];
				_stream.FullRead(result);
				return result;
			}
			private byte _ReadByte()
			{
				int result = _stream.ReadByte();
				
				if (result == -1)
					throw new SerializationException("End-of-stream at unexpected point.");
				
				return (byte)result;
			}
			
			private int _ReadCompressedInt()
			{
				var result = 0;
				var bitShift = 0;

				while(true)
				{
					byte nextByte = _ReadByte();

					result |= (nextByte & 0x7f) << bitShift;
					bitShift += 7;

					if ((nextByte & 0x80) == 0)
						return result;
				}
			}
			private void _WriteCompressedInt(int value)
			{
				var unsignedValue = unchecked((uint) value);

				while(unsignedValue >= 0x80)
				{
					_stream.WriteByte((byte)(unsignedValue | 0x80));
					unsignedValue >>= 7;
				}

				_stream.WriteByte((byte)unsignedValue);
			}
			
			private void _ReadBooleanArray(bool[] array)
			{
				int count = array.Length;
				byte b = 0;
				for (int i=0; i<count; i++)
				{
					int mod = i % 8;
					if (mod == 0)
						b = _ReadByte();
					
					array[i] = ((b << mod) & 128) == 128;
				}
			}
			private void _WriteBooleanArray(bool[] array)
			{
				int count = array.Length;
				byte b = 0;
				for (int i=0; i<count; i++)
				{
					int mod = i % 8;

					if (array[i])
						b |= (byte)(128 >> mod);

					if (mod == 7)
					{
						_stream.WriteByte(b);
						b = 0;
					}
				}
				
				if ((count % 8) != 0)
					_stream.WriteByte(b);
			}
			private void _ReadNullableBooleanArray(bool?[] array)
			{
				int count = array.Length;
				byte b = 0;
				for (int i=0; i<count; i++)
				{
					int mod = (i % 4) * 2;
					if (mod == 0)
						b = _ReadByte();
					
					switch((b << mod) & 192)
					{
						case 192:
							array[i] = null;
							break;
						
						case 0:
							array[i] = false;
							break;
						
						case 64:
							array[i] = true;
							break;
						
						default:
							throw new SerializationException("Invalid byte in array.");
					}
				}
			}
			private void _WriteNullableBooleanArray(bool?[] array)
			{
				int count = array.Length;
				byte b = 0;
				for (int i=0; i<count; i++)
				{
					int mod = (i % 4) * 2;

					bool? value = array[i];
					if (value.HasValue)
					{
						if (value.Value)
							b |= (byte)(64 >> mod);
							
						// there is no need for else, as we will combine 0 to it.
					}
					else
						b |= (byte)(192 >> mod);

					if (mod == 6)
					{
						_stream.WriteByte(b);
						b = 0;
					}
				}
				
				if ((count % 4) != 0)
					_stream.WriteByte(b);
			}
		#endregion

		#region IFormatter Members - Not used, but here to keep some compatibility.
			SerializationBinder IFormatter.Binder
			{
				get
				{
					return null;
				}
				set
				{
					if (value != null)
						throw new NotSupportedException("BinarySerializer does not support setting a Binder.");
				}
			}
			ISurrogateSelector IFormatter.SurrogateSelector
			{
				get
				{
					return null;
				}
				set
				{
					if (value != null)
						throw new NotSupportedException("BinarySerializer does not support setting a SurrogateSelector.");
				}
			}
		#endregion
	}
}
