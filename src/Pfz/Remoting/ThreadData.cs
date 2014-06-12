using System;
using Pfz.Remoting.Instructions;
using Pfz.Serialization;
using System.Collections.Generic;
using System.Reflection;

namespace Pfz.Remoting
{
	internal sealed class ThreadData
	{
		private readonly RemotingSerializer Serializer;
		private readonly ExceptionAwareStream Channel;

		public ThreadData(ExceptionAwareStream channel, RemotingClient client)
		{
			Channel = channel;
			Serializer = new RemotingSerializer(client);
				
			Serializer.AddRecommendedDefaults();

			Serializer.AddDefaultType(typeof(long[]));
			Serializer.AddDefaultType(typeof(Type[]));

			Serializer.AddDefaultType(typeof(InstructionCreateObject));
			Serializer.AddDefaultType(typeof(InstructionAddEvent));
			Serializer.AddDefaultType(typeof(InstructionRemoveEvent));
			Serializer.AddDefaultType(typeof(InstructionGetProperty));
			Serializer.AddDefaultType(typeof(InstructionInvokeMethod));
			Serializer.AddDefaultType(typeof(InstructionInvokeStaticMethod));
			Serializer.AddDefaultType(typeof(InstructionObjectsCollected));
			Serializer.AddDefaultType(typeof(InstructionSetProperty));
			Serializer.AddDefaultType(typeof(InstructionRemoveReferencesNotFound));
			Serializer.AddDefaultType(typeof(InstructionInvokeDelegate));

			Serializer.AddDefaultType(typeof(Reference));
			Serializer.AddDefaultType(typeof(Wrapped));
			Serializer.AddDefaultType(typeof(WrappedDelegate));
			Serializer.AddDefaultType(typeof(RemotingResult));
			Serializer.AddDefaultType(typeof(BackObjectReference));
			Serializer.AddDefaultType(typeof(BackDelegateReference));
		}

		private object _lastData;
		private bool _canReconnect;
		public void Serialize(bool canReconnect, object data)
		{
			_canReconnect = canReconnect;
			_lastData = data;

			Serializer.Serialize(canReconnect, Channel, data);
			Channel.Flush();
		}
		public void SerializeLastData()
		{
			Serializer.Serialize(_canReconnect, Channel, _lastData);
			Channel.Flush();
		}
		public object Deserialize()
		{
			return Serializer.Deserialize(Channel);
		}

		public bool LastDeserializeAllowReconnect
		{
			get
			{
				return Serializer._canReconnect;
			}
		}
		public int LastDeserializeWrapCount
		{
			get
			{
				return Serializer._wrapCount;
			}
		}

		internal void _Action(bool canReconnect, Func<object> action)
		{
			var result = new RemotingResult();
			try
			{
				result.Value = action();
			}
			catch(Exception exception)
			{
				result.Exception = exception;
			}

			Serialize(canReconnect, result);
		}
		internal void _Action(bool canReconnect, MethodInfo methodInfo, object[] outParameters, Func<object> action)
		{
			var result = new RemotingResult();
			try
			{
				result.Value = action();
				result.OutValues = RemotingClient._GetOutValues(methodInfo, outParameters);
			}
			catch(Exception exception)
			{
				result.Exception = exception;
			}

			Serialize(canReconnect, result);
		}
	}
}
