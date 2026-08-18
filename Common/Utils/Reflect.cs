using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace TerraStorageOverflow.Common.Utils
{
    /// <summary>
    /// Reflection helper utility for locating members, bypassing access flags, and creating delegates/invocations.
    /// </summary>
    public static class Reflect
    {
        /// <summary>
        /// BindingFlags combination covering Public, NonPublic, Instance, and Static members.
        /// </summary>
        public const BindingFlags Any =
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static;

        // Base Type Walking Helpers

        /// <summary>
        /// Finds a field on the specified type or any of its base classes.
        /// </summary>
        /// <param name="type">Target type to search.</param>
        /// <param name="name">Name of the field.</param>
        /// <returns>The <see cref="FieldInfo"/> if found; otherwise, <c>null</c>.</returns>
        public static FieldInfo Field(Type type, string name)
        {
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                var field = t.GetField(name, Any);
                if (field != null)
                    return field;
            }
            return null;
        }

        /// <summary>
        /// Finds a field on <typeparamref name="T"/> or any of its base classes.
        /// </summary>
        /// <typeparam name="T">Target type to search.</typeparam>
        /// <param name="name">Name of the field.</param>
        /// <returns>The <see cref="FieldInfo"/> if found; otherwise, <c>null</c>.</returns>
        public static FieldInfo Field<T>(string name)
        {
            return Field(typeof(T), name);
        }

        /// <summary>
        /// Finds a property on the specified type or any of its base classes.
        /// </summary>
        /// <param name="type">Target type to search.</param>
        /// <param name="name">Name of the property.</param>
        /// <returns>The <see cref="PropertyInfo"/> if found; otherwise, <c>null</c>.</returns>
        public static PropertyInfo Property(Type type, string name)
        {
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                var prop = t.GetProperty(name, Any);
                if (prop != null)
                    return prop;
            }
            return null;
        }

        /// <summary>
        /// Finds a property on <typeparamref name="T"/> or any of its base classes.
        /// </summary>
        /// <typeparam name="T">Target type to search.</typeparam>
        /// <param name="name">Name of the property.</param>
        /// <returns>The <see cref="PropertyInfo"/> if found; otherwise, <c>null</c>.</returns>
        public static PropertyInfo Property<T>(string name)
        {
            return Property(typeof(T), name);
        }

        /// <summary>
        /// Resolves the getter <see cref="MethodInfo"/> of a property, including non-public getters.
        /// </summary>
        /// <param name="type">Target type.</param>
        /// <param name="name">Name of the property.</param>
        /// <returns>The getter method if found; otherwise, <c>null</c>.</returns>
        public static MethodInfo PropertyGetter(Type type, string name)
        {
            return Property(type, name)?.GetGetMethod(true);
        }

        /// <summary>
        /// Resolves the setter <see cref="MethodInfo"/> of a property, including non-public setters.
        /// </summary>
        /// <param name="type">Target type.</param>
        /// <param name="name">Name of the property.</param>
        /// <returns>The setter method if found; otherwise, <c>null</c>.</returns>
        public static MethodInfo PropertySetter(Type type, string name)
        {
            return Property(type, name)?.GetSetMethod(true);
        }

        // Method Identification

        /// <summary>
        /// Finds a method by name and optional parameter signature, walking base types if needed.
        /// </summary>
        /// <param name="type">Target type to search.</param>
        /// <param name="name">Name of the method.</param>
        /// <param name="paramTypes">Optional parameter types to match overloads.</param>
        /// <returns>The matching <see cref="MethodInfo"/> if found; otherwise, <c>null</c>.</returns>
        public static MethodInfo Method(Type type, string name, params Type[] paramTypes)
        {
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                if (paramTypes != null)
                {
                    try
                    {
                        var matched = t.GetMethod(name, Any, null, paramTypes, null);
                        if (matched != null)
                            return matched;
                    }
                    catch (AmbiguousMatchException) { }
                }

                var candidates = t.GetMethods(Any).Where(m => m.Name == name).ToArray();
                if (candidates.Length == 1)
                    return candidates[0];

                if (paramTypes != null)
                {
                    var match = candidates.FirstOrDefault(m =>
                        m.GetParameters().Select(p => p.ParameterType).SequenceEqual(paramTypes)
                    );
                    if (match != null)
                        return match;
                }
                else if (candidates.Length > 0)
                {
                    return candidates[0];
                }
            }
            return null;
        }

        /// <summary>
        /// Finds a method on <typeparamref name="T"/> by name and optional parameter signature.
        /// </summary>
        /// <typeparam name="T">Target type to search.</typeparam>
        /// <param name="name">Name of the method.</param>
        /// <param name="paramTypes">Optional parameter types to match overloads.</param>
        /// <returns>The matching <see cref="MethodInfo"/> if found; otherwise, <c>null</c>.</returns>
        public static MethodInfo Method<T>(string name, params Type[] paramTypes)
        {
            return Method(typeof(T), name, paramTypes);
        }

        /// <summary>
        /// Resolves a generic method definition and constructs a closed generic method.
        /// </summary>
        /// <param name="type">Target type.</param>
        /// <param name="name">Name of the generic method.</param>
        /// <param name="genericTypes">Generic type arguments to bind.</param>
        /// <param name="paramTypes">Optional parameter types to resolve overloaded generic definitions.</param>
        /// <returns>A closed-bound <see cref="MethodInfo"/> if resolved; otherwise, <c>null</c>.</returns>
        public static MethodInfo GenericMethod(
            Type type,
            string name,
            Type[] genericTypes,
            params Type[] paramTypes
        )
        {
            var method = Method(type, name, paramTypes);
            return method?.IsGenericMethodDefinition == true
                ? method.MakeGenericMethod(genericTypes)
                : method;
        }

        // Expression Retrieval

        /// <summary>
        /// Extracts the target <see cref="MethodInfo"/> from a lambda method call expression.
        /// </summary>
        /// <param name="expr">Lambda expression calling a method (e.g. <c>() =&gt; Target()</c>).</param>
        /// <returns>The called <see cref="MethodInfo"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if the expression is not a direct method call.</exception>
        public static MethodInfo MethodOf(LambdaExpression expr)
        {
            return expr.Body is MethodCallExpression mce ? mce.Method
                : expr.Body is UnaryExpression { Operand: MethodCallExpression umce } ? umce.Method
                : throw new ArgumentException("Expression must be a direct method call.");
        }

        // Direct Access

        /// <summary>
        /// Reads the value of a field, property, or compiler-generated backing field on a static or instance target.
        /// </summary>
        /// <typeparam name="T">Expected return type.</typeparam>
        /// <param name="target">Instance object, or <see cref="Type"/> for static members.</param>
        /// <param name="memberName">Name of the field or property.</param>
        /// <returns>The member value cast to <typeparamref name="T"/>.</returns>
        /// <exception cref="MissingMemberException">Thrown if no matching field or property is found.</exception>
        public static T GetValue<T>(object target, string memberName)
        {
            var type = target is Type t ? t : target.GetType();
            var instance = target is Type ? null : target;

            var field = Field(type, memberName) ?? Field(type, $"<{memberName}>k__BackingField");
            if (field != null)
                return (T)field.GetValue(instance);

            var prop = Property(type, memberName);
            return prop != null
                ? (T)prop.GetValue(instance)
                : throw new MissingMemberException(type.FullName, memberName);
        }

        /// <summary>
        /// Sets the value of a field, property, or read-only auto-property backing field on a target.
        /// </summary>
        /// <param name="target">Instance object, or <see cref="Type"/> for static members.</param>
        /// <param name="memberName">Name of the field or property.</param>
        /// <param name="value">New value to set.</param>
        /// <exception cref="MissingMemberException">Thrown if no matching field or property is found.</exception>
        public static void SetValue(object target, string memberName, object value)
        {
            var type = target is Type t ? t : target.GetType();
            var instance = target is Type ? null : target;

            var field = Field(type, memberName) ?? Field(type, $"<{memberName}>k__BackingField");
            if (field != null)
            {
                field.SetValue(instance, value);
                return;
            }

            var prop = Property(type, memberName);
            if (prop != null)
            {
                if (prop.CanWrite)
                    prop.SetValue(instance, value);
                else
                    Field(type, $"<{memberName}>k__BackingField")?.SetValue(instance, value);
                return;
            }

            throw new MissingMemberException(type.FullName, memberName);
        }

        // Delegate Creation & Invocation

        /// <summary>
        /// Binds a method to a strongly-typed delegate.
        /// </summary>
        /// <typeparam name="TDelegate">Delegate type to create.</typeparam>
        /// <param name="type">Declaring type.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="instance">Target instance for instance methods, or <c>null</c> for static methods.</param>
        /// <param name="paramTypes">Optional parameter types to resolve overloads.</param>
        /// <returns>A strongly-typed delegate instance.</returns>
        /// <exception cref="MissingMethodException">Thrown if the method cannot be found.</exception>
        public static TDelegate CreateDelegate<TDelegate>(
            Type type,
            string methodName,
            object instance = null,
            params Type[] paramTypes
        )
            where TDelegate : Delegate
        {
            var mi = Method(type, methodName, paramTypes);
            return mi == null
                ? throw new MissingMethodException(type.FullName, methodName)
                : (TDelegate)mi.CreateDelegate(typeof(TDelegate), instance);
        }

        /// <summary>
        /// Resolves and invokes a method on a target object or static type with given arguments.
        /// </summary>
        /// <param name="target">Instance object, or <see cref="Type"/> for static methods.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="args">Arguments to pass to the method.</param>
        /// <returns>The return value of the method, or <c>null</c> if void.</returns>
        /// <exception cref="MissingMethodException">Thrown if a matching method signature cannot be found.</exception>
        public static object Invoke(object target, string methodName, params object[] args)
        {
            var type = target is Type t ? t : target.GetType();
            var instance = target is Type ? null : target;
            var paramTypes =
                args?.Select(a => a?.GetType() ?? typeof(object)).ToArray() ?? Type.EmptyTypes;

            var method = Method(type, methodName, paramTypes);
            return method == null
                ? throw new MissingMethodException(type.FullName, methodName)
                : method.Invoke(instance, args);
        }
    }
}
