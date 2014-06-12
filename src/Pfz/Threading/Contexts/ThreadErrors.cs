using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Pfz.DataTypes;
using Pfz.Extensions;

namespace Pfz.Threading.Contexts
{
	/// <summary>
	/// Use this class if you want to "pack" many database errors at once, or
	/// use it's statics methods to add, get or clear errors.
	/// </summary>
	public sealed class ThreadErrors:
		IDisposable
	{
		[ThreadStatic]
		private static object _threadRecord;
	
		[ThreadStatic]
		private static Dictionary<object, HashSet<object>> _errors;

		[ThreadStatic]
		private static HashSet<object> _errorsHashSet;
		
		[ThreadStatic]
		private static ThreadErrors _owner;
		
		private object _oldRecord;
		
		/// <summary>
		/// Runs the given action inside an abort-safe using with ThreadErrors.
		/// </summary>
		public static void Run(Action action)
		{
			Run(default(object), action);
		}
		
		/// <summary>
		/// Runs the given action inside an abort-safe using ThreadErrors and
		/// active record.
		/// </summary>
		public static void Run(object record, Action action)
		{
			ThreadErrors errors = null;
			AbortSafe.Run
			(
				() => errors = new ThreadErrors(record),
				action,
				() => errors.CheckedDispose()
			);
		}
		
		/// <summary>
		/// Creates a new database error "pack".
		/// </summary>
		public ThreadErrors(object record=null)
		{
			if (_owner == null)
			{
				_errors = new Dictionary<object, HashSet<object>>();
				_errorsHashSet = new HashSet<object>();
				_owner = this;
			}
			else
				_oldRecord = _threadRecord;
			
			_threadRecord = record;
		}
		
		/// <summary>
		/// Disposes this object and maybe throws and exception if there
		/// are errors.
		/// </summary>
		public void Dispose()
		{
			_threadRecord = _oldRecord;
			
			if (_owner == this)
			{
				_owner = null;
				var errors = _errors;
				var errorsHashSet = _errorsHashSet;
				_errors = null;
				
				if (errorsHashSet.Count > 0 || errors.Count > 0)
					OnThrowingException(errorsHashSet, errors);
			}
		}
		
		/// <summary>
		/// Gets a value indicating if there are errors packed.
		/// </summary>
		public static bool HasErrors
		{
			get
			{
				return _errors != null && (_errors.Count > 0 || _errorsHashSet.Count > 0);
			}
		}
		
		/// <summary>
		/// Gets the errors that are not specific to a data-context.
		/// </summary>
		/// <returns></returns>
		public static HashSet<object> GetGlobalErrors()
		{
			return _errorsHashSet;
		}

		/// <summary>
		/// Gets the dictionary with the errors.
		/// </summary>
		public static Dictionary<object, HashSet<object>> GetErrorsDictionary()
		{
			return _errors;
		}
		
		/// <summary>
		/// Clear all the errors.
		/// </summary>
		public static void ClearErrors()
		{
			if (_errors != null)
			{
				_errors = new Dictionary<object, HashSet<object>>();
				_errorsHashSet = new HashSet<object>();
			}
		}
		
		/// <summary>
		/// Adds an error to the packet, or throws an exception immediatelly.
		/// In general errors are string, but you can add any object if you
		/// know how to use it later.
		/// </summary>
		public static void AddError(object error)
		{
			if (error == null)
				throw new ArgumentNullException("error");
		
			HashSet<object> hashset;
			if (_errors == null)
			{
				hashset = new HashSet<object>();
				hashset.Add(error);

				var dictionary = new Dictionary<object, HashSet<object>>();
				OnThrowingException(hashset, dictionary);
			}
			
			if (_threadRecord == null)
			{
				_errorsHashSet.Add(error);
				return;
			}

			if (!_errors.TryGetValue(_threadRecord, out hashset))
			{
				hashset = new HashSet<object>();
				_errors.Add(_threadRecord, hashset);
			}
			
			hashset.Add(error);
		}
		/// <summary>
		/// Adds a PropertyError object to the error list.
		/// </summary>
		public static void AddError(PropertyInfo propertyInfo, object error)
		{
			AddError(new PropertyError(propertyInfo, error));
		}
		
		/// <summary>
		/// Adds a PropertyError object to the error list.
		/// Uses an expression to know what propertyInfo to use.
		/// </summary>
		public static void AddError<T>(Expression<Func<T>> getPropertyExpression, object error)
		{
			var propertyInfo = ReflectionHelper.GetProperty(getPropertyExpression);
			AddError(propertyInfo, error);
		}

		/// <summary>
		/// Gets a single string with all errors.
		/// </summary>
		public static string GetSimpleAllErrorsMessage()
		{
			if (_errors == null || (_errors.Count == 0 && _errorsHashSet.Count == 0))
				return null;
			
			return _GetSimpleAllErrorsMessage(_errors);
		}

		internal static string _GetSimpleAllErrorsMessage(Dictionary<object, HashSet<object>> dictionary)
		{
			StringBuilder result = new StringBuilder();

			foreach(var error in _errorsHashSet)
			{
				result.Append(error);
				result.Append("\r\n");
			}
			
			foreach(var list in dictionary.Values)
			{
				foreach(var error in list)
				{
					result.Append(error);
					result.Append("\r\n");
				}
			}
					
			return result.ToString();
		}

		/// <summary>
		/// Event invoked when an exception is about to be thrown.
		/// </summary>
		public static event Action<ThrowingExceptionEventArgs> ThrowingException;

		private static void OnThrowingException(HashSet<object> globalErrors, Dictionary<object, HashSet<object>> errorsDictionary)
		{
			var throwing = ThrowingException;
			if (throwing == null)
				throw new ValidationException(globalErrors, errorsDictionary);

			var args = new ThrowingExceptionEventArgs();
			args.ErrorsDictionary = errorsDictionary;
			args.GlobalErrors = globalErrors;
			throwing(args);

			if (!args.WasHandled)
				throw new ValidationException(globalErrors, errorsDictionary);
		}
	}
}
