using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Pfz.Threading.Contexts
{
	/// <summary>
	/// Exception that is thrown by DatabaseValidationErrors class.
	/// </summary>
	[Serializable]
	public class ValidationException:
		Exception
	{
		#region MessageFormats for Errors
			private static string _messageFormatManyErrorsInSingleRecord = "You have a total of {0} errors in 1 record.";
			/// <summary>
			/// Gets or sets the format of the message used when there are many errors
			/// in a single record.
			/// {0} is the number of errors.
			/// </summary>
			public static string MessageFormatManyErrorsInSingleRecord
			{
				get
				{
					return _messageFormatManyErrorsInSingleRecord;
				}
				set
				{
					_messageFormatManyErrorsInSingleRecord = value;
				}
			}
			
			private static string _messageFormatManyErrorsInManyRecords = "You have a total of {0} errors in {1} records.";
			/// <summary>
			/// Gets or sets the message format used when there are many errors,
			/// in many records.
			/// {0} is the number of errors.
			/// {1} is the number of records.
			/// </summary>
			public static string MessageFormatManyErrorsInManyRecords
			{
				get
				{
					return _messageFormatManyErrorsInManyRecords;
				}
				set
				{
					_messageFormatManyErrorsInManyRecords = value;
				}
			}
		#endregion
	
		/// <summary>
		/// Exception pattern.
		/// </summary>
		public ValidationException()
		{
		}

		/// <summary>
		/// Exception pattern.
		/// </summary>
		public ValidationException(string message):
			base(message)
		{
		}

		/// <summary>
		/// Exception pattern.
		/// </summary>
		public ValidationException(string message, Exception inner):
			base(message, inner)
		{
		}

		/// <summary>
		/// Reads the ValidationErrorsDictionary.
		/// </summary>
		protected ValidationException(SerializationInfo info, StreamingContext context):
			base(info, context)
		{
			_errorsDictionary = (Dictionary<object, HashSet<object>>)info.GetValue("ErrorsDictionary", typeof(Dictionary<object, HashSet<object>>));
			_globalErrors = (HashSet<object>)info.GetValue("GlobalErrors", typeof(HashSet<object>));
		}
		
		/// <summary>
		/// Restores the validation error dictionary.
		/// </summary>
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
 			 base.GetObjectData(info, context);
 			 
 			 info.AddValue("ErrorsDictionary", _errorsDictionary);
			 info.AddValue("GlobalErrors", _globalErrors);
		}
		
		/// <summary>
		/// Creates the exception based on a validation errors dictionary.
		/// </summary>
		public ValidationException(HashSet<object> globalErrors, Dictionary<object, HashSet<object>> validationErrors):
			base(_CreateMessage(globalErrors, validationErrors))
		{
			_globalErrors = globalErrors;
			_errorsDictionary = validationErrors;
		}

		private static string _CreateMessage(HashSet<object> globalHashSet, Dictionary<object, HashSet<object>> validationErrors)
		{
			int errorCount = globalHashSet.Count;
			foreach(var hashset in validationErrors.Values)
				errorCount += hashset.Count;
				
			if (errorCount == 1)
			{
				foreach(var message in globalHashSet)
					return message.ToString();

				foreach(var hashset in validationErrors.Values)
					foreach(var message in hashset)
						return message.ToString();
			}
			
			int recordCount = validationErrors.Count;
			if (globalHashSet.Count > 0)
				recordCount++;

			if (recordCount == 1)
				return string.Format(_messageFormatManyErrorsInSingleRecord, errorCount);

			return string.Format(_messageFormatManyErrorsInManyRecords, errorCount, recordCount);
		}

		private HashSet<object> _globalErrors;
		/// <summary>
		/// Gets a hashset with errors not bound to an specific data-source.
		/// </summary>
		public HashSet<object> GlobalErrors
		{
			get
			{
				return _globalErrors;
			}
		}
		
		private Dictionary<object, HashSet<object>> _errorsDictionary;
		/// <summary>
		/// Gets the dictionary with the errors that caused this exception.
		/// </summary>
		public Dictionary<object, HashSet<object>> ErrorsDictionary
		{
			get
			{
				return _errorsDictionary;
			}
		}
		
		/// <summary>
		/// Gets all errors as a single message.
		/// </summary>
		public string GetSimpleAllErrorsMessage()
		{
			if (_errorsDictionary == null || _errorsDictionary.Count == 0)
				return Message;
		
			return ThreadErrors._GetSimpleAllErrorsMessage(_errorsDictionary);
		}
	}
}
