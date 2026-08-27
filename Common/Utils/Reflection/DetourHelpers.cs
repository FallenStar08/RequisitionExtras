using System;
using System.Linq.Expressions;
using Terraria.ModLoader;

namespace TerraStorageOverflow.Common.Utils.Reflection
{
    internal static class DetourHelpers
    {
        /// <summary>
        /// Applies a detour to the specified method using <see cref="MonoModHooks.Add"/>.
        /// </summary>
        public static void Detour(
            Type type,
            string methodName,
            Delegate hook,
            params Type[] paramTypes
        )
        {
            var target =
                Reflect.Method(type, methodName, paramTypes)
                ?? throw new MissingMethodException(type.FullName, methodName);
            MonoModHooks.Add(target, hook);
        }

        /// <summary>
        /// Applies a detour to the specified method on <typeparamref name="T"/> using <see cref="MonoModHooks.Add"/>.
        /// </summary>
        public static void Detour<T>(string methodName, Delegate hook, params Type[] paramTypes)
        {
            Detour(typeof(T), methodName, hook, paramTypes);
        }

        /// <summary>
        /// Applies a detour to a method resolved from a lambda expression using <see cref="MonoModHooks.Add"/>.
        /// </summary>
        public static void Detour(LambdaExpression targetExpr, Delegate hook)
        {
            var target = Reflect.MethodOf(targetExpr);
            MonoModHooks.Add(target, hook);
        }

        /// <summary>Detours an instance method taking 0 arguments and returning void.</summary>
        public static void Detour<TTarget>(string name, Action<Action<TTarget>, TTarget> hook)
        {
            Detour(typeof(TTarget), name, hook, Type.EmptyTypes);
        }

        /// <summary>Detours an instance method taking 1 argument and returning void.</summary>
        public static void Detour<TTarget, T1>(
            string name,
            Action<Action<TTarget, T1>, TTarget, T1> hook
        )
        {
            Detour(typeof(TTarget), name, hook, typeof(T1));
        }

        /// <summary>Detours an instance method taking 2 arguments and returning void.</summary>
        public static void Detour<TTarget, T1, T2>(
            string name,
            Action<Action<TTarget, T1, T2>, TTarget, T1, T2> hook
        )
        {
            Detour(typeof(TTarget), name, hook, typeof(T1), typeof(T2));
        }

        /// <summary>Detours an instance method taking 3 arguments and returning void.</summary>
        public static void Detour<TTarget, T1, T2, T3>(
            string name,
            Action<Action<TTarget, T1, T2, T3>, TTarget, T1, T2, T3> hook
        )
        {
            Detour(typeof(TTarget), name, hook, typeof(T1), typeof(T2), typeof(T3));
        }

        /// <summary>Detours an instance method taking 4 arguments and returning void.</summary>
        public static void Detour<TTarget, T1, T2, T3, T4>(
            string name,
            Action<Action<TTarget, T1, T2, T3, T4>, TTarget, T1, T2, T3, T4> hook
        )
        {
            Detour(typeof(TTarget), name, hook, typeof(T1), typeof(T2), typeof(T3), typeof(T4));
        }

        /// <summary>Detours an instance method taking 0 arguments and returning <typeparamref name="TResult"/>.</summary>
        public static void Detour<TTarget, TResult>(
            string name,
            Func<Func<TTarget, TResult>, TTarget, TResult> hook
        )
        {
            Detour(typeof(TTarget), name, hook, Type.EmptyTypes);
        }

        /// <summary>Detours an instance method taking 1 argument and returning <typeparamref name="TResult"/>.</summary>
        public static void Detour<TTarget, T1, TResult>(
            string name,
            Func<Func<TTarget, T1, TResult>, TTarget, T1, TResult> hook
        )
        {
            Detour(typeof(TTarget), name, hook, typeof(T1));
        }

        /// <summary>Detours an instance method taking 2 arguments and returning <typeparamref name="TResult"/>.</summary>
        public static void Detour<TTarget, T1, T2, TResult>(
            string name,
            Func<Func<TTarget, T1, T2, TResult>, TTarget, T1, T2, TResult> hook
        )
        {
            Detour(typeof(TTarget), name, hook, typeof(T1), typeof(T2));
        }

        /// <summary>Detours an instance method taking 3 arguments and returning <typeparamref name="TResult"/>.</summary>
        public static void Detour<TTarget, T1, T2, T3, TResult>(
            string name,
            Func<Func<TTarget, T1, T2, T3, TResult>, TTarget, T1, T2, T3, TResult> hook
        )
        {
            Detour(typeof(TTarget), name, hook, typeof(T1), typeof(T2), typeof(T3));
        }

        /// <summary>Detours a static method taking 0 arguments and returning void.</summary>
        public static void DetourStatic(Type type, string name, Action<Action> hook)
        {
            Detour(type, name, hook, Type.EmptyTypes);
        }

        /// <summary>Detours a static method taking 1 argument and returning void.</summary>
        public static void DetourStatic<T1>(Type type, string name, Action<Action<T1>, T1> hook)
        {
            Detour(type, name, hook, typeof(T1));
        }

        /// <summary>Detours a static method taking 2 arguments and returning void.</summary>
        public static void DetourStatic<T1, T2>(
            Type type,
            string name,
            Action<Action<T1, T2>, T1, T2> hook
        )
        {
            Detour(type, name, hook, typeof(T1), typeof(T2));
        }

        /// <summary>Detours a static method taking 0 arguments and returning <typeparamref name="TResult"/>.</summary>
        public static void DetourStatic<TResult>(
            Type type,
            string name,
            Func<Func<TResult>, TResult> hook
        )
        {
            Detour(type, name, hook, Type.EmptyTypes);
        }

        /// <summary>Detours a static method taking 1 argument and returning <typeparamref name="TResult"/>.</summary>
        public static void DetourStatic<T1, TResult>(
            Type type,
            string name,
            Func<Func<T1, TResult>, T1, TResult> hook
        )
        {
            Detour(type, name, hook, typeof(T1));
        }

        /// <summary>Detours a static method taking 2 arguments and returning <typeparamref name="TResult"/>.</summary>
        public static void DetourStatic<T1, T2, TResult>(
            Type type,
            string name,
            Func<Func<T1, T2, TResult>, T1, T2, TResult> hook
        )
        {
            Detour(type, name, hook, typeof(T1), typeof(T2));
        }
    }
}
