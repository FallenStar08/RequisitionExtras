using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace TerraStorageOverflow.Common.Utils
{
    public static class Reflect
    {
        public const BindingFlags Any =
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static;

        // Base Type Walking Helpers

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

        public static FieldInfo Field<T>(string name)
        {
            return Field(typeof(T), name);
        }

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

        public static PropertyInfo Property<T>(string name)
        {
            return Property(typeof(T), name);
        }

        // Getter / Setter MethodInfo resolution for hooks
        public static MethodInfo PropertyGetter(Type type, string name)
        {
            return Property(type, name)?.GetGetMethod(true);
        }

        public static MethodInfo PropertySetter(Type type, string name)
        {
            return Property(type, name)?.GetSetMethod(true);
        }

        // Method Identification

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

        public static MethodInfo Method<T>(string name, params Type[] paramTypes)
        {
            return Method(typeof(T), name, paramTypes);
        }

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

        public static MethodInfo MethodOf(LambdaExpression expr)
        {
            return expr.Body is MethodCallExpression mce ? mce.Method
                : expr.Body is UnaryExpression { Operand: MethodCallExpression umce } ? umce.Method
                : throw new ArgumentException("Expression must be a direct method call.");
        }

        // Direct Access

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
