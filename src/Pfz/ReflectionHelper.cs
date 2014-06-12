using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Pfz
{
	/// <summary>
	/// This class allows you to get members from types more safely than using
	/// string literals. It only exists because C# does not have fieldinfoof,
	/// propertyinfoof and methodinfoof.
	/// </summary>
	public static class ReflectionHelper
	{
		#region GetMember
			/// <summary>
			/// Gets a member by it's expression usage.
			/// For example, GetMember(() => obj.GetType()) will return the
			/// GetType method.
			/// </summary>
			public static MemberInfo GetMember<T>(Expression<Func<T>> expression)
			{
				if (expression == null)
					throw new ArgumentNullException("expression");
				
				var body = expression.Body;
			
				switch(body.NodeType)
				{
					case ExpressionType.MemberAccess:
						MemberExpression memberExpression = (MemberExpression)body;
						return memberExpression.Member;
				
					case ExpressionType.Call:
						MethodCallExpression callExpression = (MethodCallExpression)body;
						return callExpression.Method;
				
					case ExpressionType.New:
						NewExpression newExpression = (NewExpression)body;
						return newExpression.Constructor;
				}
			
				throw new ArgumentException("expression.Body must be a member or call expression.", "expression");
			}
		#endregion
		#region GetConstructor
			/// <summary>
			/// Gets the constructor info from a sample construction call expression.
			/// Example: GetConstructor(() => new Control()) will return the constructor
			/// info for the default constructor of Control.
			/// </summary>
			public static ConstructorInfo GetConstructor<T>(Expression<Func<T>> expression)
			{
				return (ConstructorInfo)GetMember(expression);
			}
		#endregion
		#region GetField
			/// <summary>
			/// Gets a field from a sample usage.
			/// Example: GetField(() => Type.EmptyTypes) will return the FieldInfo of
			/// EmptyTypes.
			/// </summary>
			public static FieldInfo GetField<T>(Expression<Func<T>> expression)
			{
				return (FieldInfo)GetMember(expression);
			}
		#endregion
		#region GetProperty
			/// <summary>
			/// Gets a property from a sample usage.
			/// Example: GetProperty(() => str.Length) will return the property info 
			/// of Length.
			/// </summary>
			public static PropertyInfo GetProperty<T>(Expression<Func<T>> expression)
			{
				return (PropertyInfo)GetMember(expression);
			}
		#endregion
		#region GetMethod
			/// <summary>
			/// Gets a method info of a void method.
			/// Example: GetMethod(() => Console.WriteLine("")); will return the
			/// MethodInfo of WriteLine that receives a single argument.
			/// </summary>
			public static MethodInfo GetMethod(Expression<Action> expression)
			{
				if (expression == null)
					throw new ArgumentNullException("expression");
			
				var body = expression.Body;
				if (body.NodeType != ExpressionType.Call)
					throw new ArgumentException("expression.Body must be a Call expression.", "expression");
			
				MethodCallExpression callExpression = (MethodCallExpression)body;
				return callExpression.Method;
			}
		
			/// <summary>
			/// Gets the MethodInfo of a method that returns a value.
			/// Example: GetMethod(() => Console.ReadLine()); will return the method info
			/// of ReadLine.
			/// </summary>
			public static MethodInfo GetMethod<T>(Expression<Func<T>> expression)
			{
				return (MethodInfo)GetMember(expression);
			}
		#endregion
	}

	/// <summary>
	/// This is a typed version of reflection helper, so your expression already starts with a know
	/// object type (used when you don't have an already instantiated object).
	/// </summary>
	public static class ReflectionHelper<ForType>
	{
		#region GetMember
			/// <summary>
			/// Gets a member by it's expression usage.
			/// For example, GetMember((obj) => obj.GetType()) will return the
			/// GetType method.
			/// </summary>
			public static MemberInfo GetMember<T>(Expression<Func<ForType, T>> expression)
			{
				if (expression == null)
					throw new ArgumentNullException("expression");
				
				var body = expression.Body;
			
				switch(body.NodeType)
				{
					case ExpressionType.MemberAccess:
						MemberExpression memberExpression = (MemberExpression)body;
						return memberExpression.Member;
				
					case ExpressionType.Call:
						MethodCallExpression callExpression = (MethodCallExpression)body;
						return callExpression.Method;
				
					case ExpressionType.New:
						NewExpression newExpression = (NewExpression)body;
						return newExpression.Constructor;
				}
			
				throw new ArgumentException("expression.Body must be a member or call expression.", "expression");
			}
		#endregion
		#region GetField
			/// <summary>
			/// Gets a field from a sample usage.
			/// Example: GetField((obj) => obj.SomeField) will return the FieldInfo of
			/// EmptyTypes.
			/// </summary>
			public static FieldInfo GetField<T>(Expression<Func<ForType, T>> expression)
			{
				return (FieldInfo)GetMember(expression);
			}
		#endregion
		#region GetProperty
			/// <summary>
			/// Gets a property from a sample usage.
			/// Example: GetProperty((str) => str.Length) will return the property info 
			/// of Length.
			/// </summary>
			public static PropertyInfo GetProperty<T>(Expression<Func<ForType, T>> expression)
			{
				return (PropertyInfo)GetMember(expression);
			}
		#endregion
		#region GetMethod
			/// <summary>
			/// Gets a method info of a void method.
			/// Example: GetMethod((obj) => obj.SomeCall("")); will return the
			/// MethodInfo of SomeCall that receives a single argument.
			/// </summary>
			public static MethodInfo GetMethod(Expression<Action<ForType>> expression)
			{
				if (expression == null)
					throw new ArgumentNullException("expression");
			
				var body = expression.Body;
				if (body.NodeType != ExpressionType.Call)
					throw new ArgumentException("expression.Body must be a Call expression.", "expression");
			
				MethodCallExpression callExpression = (MethodCallExpression)body;
				return callExpression.Method;
			}
		
			/// <summary>
			/// Gets the MethodInfo of a method that returns a value.
			/// Example: GetMethod((obj) => obj.SomeCall()); will return the method info
			/// of SomeCall.
			/// </summary>
			public static MethodInfo GetMethod<T>(Expression<Func<ForType, T>> expression)
			{
				return (MethodInfo)GetMember(expression);
			}
		#endregion
	}
}
