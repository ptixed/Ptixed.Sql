using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Ptixed.Sql.Impl
{
    public class Accessor<TKey>
    {
        private readonly Dictionary<TKey, MemberInfo> _lookup;

        private readonly Dictionary<TKey, Func<object, object>> _getters = new Dictionary<TKey, Func<object, object>>();
        private readonly Dictionary<TKey, Action<object, object>> _setters = new Dictionary<TKey, Action<object, object>>();

        private readonly Dictionary<TKey, Func<object, object>> _methods0 = new Dictionary<TKey, Func<object, object>>();
        private readonly Dictionary<TKey, Func<object, object, object>> _methods1 = new Dictionary<TKey, Func<object, object, object>>();
        private readonly Dictionary<TKey, Func<object, object, object, object>> _methods2 = new Dictionary<TKey, Func<object, object, object, object>>();
        private readonly Dictionary<TKey, Func<object, object, object, object, object>> _methods3 = new Dictionary<TKey, Func<object, object, object, object, object>>();
        private readonly Dictionary<TKey, Func<object, object, object, object, object, object>> _methods4 = new Dictionary<TKey, Func<object, object, object, object, object, object>>();
        
        public readonly Type Type;

        public object this[object target, TKey name]
        {
            get => _getters[name](target);
            set => _setters[name](target, value);
        }

        public Accessor(Type type, Dictionary<TKey, MemberInfo> lookup)
        {
            _lookup = new Dictionary<TKey, MemberInfo>(lookup);
            
            Type = type;
            foreach (var kv in lookup)
                switch (kv.Value)
                {
                    case MethodInfo method:
                        switch (method.GetParameters().Length)
                        {
                            case 0:
                                _methods0.Add(kv.Key, Accessor.CreateCall<Func<object, object>>(method));
                                break;
                            case 1:
                                _methods1.Add(kv.Key, Accessor.CreateCall<Func<object, object, object>>(method));
                                break;
                            case 2:
                                _methods2.Add(kv.Key, Accessor.CreateCall<Func<object, object, object, object>>(method));
                                break;
                            case 3:
                                _methods3.Add(kv.Key, Accessor.CreateCall<Func<object, object, object, object, object>>(method));
                                break;
                            case 4:
                                _methods4.Add(kv.Key, Accessor.CreateCall<Func<object, object, object, object, object, object>>(method));
                                break;
                        }
                        break;
                    case ConstructorInfo constructor:
                        switch (constructor.GetParameters().Length)
                        {
                            case 0:
                                _methods0.Add(kv.Key, Accessor.CreateCall<Func<object, object>>(constructor));
                                break;
                            case 1:
                                _methods1.Add(kv.Key, Accessor.CreateCall<Func<object, object, object>>(constructor));
                                break;
                            case 2:
                                _methods2.Add(kv.Key, Accessor.CreateCall<Func<object, object, object, object>>(constructor));
                                break;
                            case 3:
                                _methods3.Add(kv.Key, Accessor.CreateCall<Func<object, object, object, object, object>>(constructor));
                                break;
                            case 4:
                                _methods4.Add(kv.Key, Accessor.CreateCall<Func<object, object, object, object, object, object>>(constructor));
                                break;
                        }
                        break;
                    case PropertyInfo property:
                        _getters.Add(kv.Key, Accessor.CreateGetter(property));
                        _setters.Add(kv.Key, Accessor.CreateSetter(property));
                        break;
                    case FieldInfo field:
                        _getters.Add(kv.Key, Accessor.CreateGetter(field));
                        _setters.Add(kv.Key, Accessor.CreateSetter(field));
                        break;
                }
        }

        public MemberInfo GetMember(TKey name) => _lookup.TryGetValue(name, out var member) ? member : null;

        public T Invoke<T>(object target, TKey name) => (T)_methods0[name](target);
        public T Invoke<T>(object target, TKey name, object p1) => (T)_methods1[name](target, p1);
        public T Invoke<T>(object target, TKey name, object p1, object p2) => (T)_methods2[name](target, p1, p2);
        public T Invoke<T>(object target, TKey name, object p1, object p2, object p3) => (T)_methods3[name](target, p1, p2, p3);
        public T Invoke<T>(object target, TKey name, object p1, object p2, object p3, object p4) => (T)_methods4[name](target, p1, p2, p3, p4);
    }

    public class Accessor
    {
        public static Func<object, object> CreateGetter(FieldInfo member)
        {
            var getter = new DynamicMethod($"{member.Name}_get_{Guid.NewGuid():N}", typeof(object), new[]
                {
                    typeof(object)
                }, member.Module, true);
            var il = getter.GetILGenerator();

            if (member.IsStatic)
            {
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ldfld, member);
                if (member.FieldType.IsValueType)
                    il.Emit(OpCodes.Box, member.FieldType);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                if (member.ReflectedType.IsValueType)
                    il.Emit(OpCodes.Unbox, member.ReflectedType);
                il.Emit(OpCodes.Ldfld, member);
                if (member.FieldType.IsValueType)
                    il.Emit(OpCodes.Box, member.FieldType);
            }

            il.Emit(OpCodes.Ret);
            return (Func<object, object>)getter.CreateDelegate(typeof(Func<object, object>));
        }

        public static Func<object, object> CreateGetter(PropertyInfo member)
        {
            var getter = new DynamicMethod(
                $"{member.Name}_get_{Guid.NewGuid():N}", 
                typeof(object), 
                new[] { typeof(object) },
                member.Module, 
                true);

            var il = getter.GetILGenerator();

            if (!member.CanRead)
                il.Emit(OpCodes.Ldnull);
            else if (member.GetAccessors(true).Any(x => x.IsStatic))
            {
                il.Emit(OpCodes.Call, member.GetMethod);
                if (member.PropertyType.IsValueType)
                    il.Emit(OpCodes.Box, member.PropertyType);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                if (member.ReflectedType.IsValueType)
                    il.Emit(OpCodes.Unbox, member.ReflectedType);
                il.Emit(member.ReflectedType.IsValueType ? OpCodes.Call : OpCodes.Callvirt, member.GetMethod);
                if (member.PropertyType.IsValueType)
                    il.Emit(OpCodes.Box, member.PropertyType);
            }

            il.Emit(OpCodes.Ret);
            return (Func<object, object>)getter.CreateDelegate(typeof(Func<object, object>));
        }

        public static Action<object, object> CreateSetter(FieldInfo member)
        {
            var setter = new DynamicMethod(
                $"{member.Name}_set_{Guid.NewGuid():N}", 
                null,
                new[] { typeof(object), typeof(object) },
                member.Module, 
                true);

            var il = setter.GetILGenerator();

            if (member.IsStatic)
            {
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ldarg_1);
                if (member.FieldType.IsValueType)
                    il.Emit(OpCodes.Unbox_Any, member.FieldType);
                il.Emit(OpCodes.Stfld, member);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                if (member.ReflectedType.IsValueType)
                    il.Emit(OpCodes.Unbox, member.ReflectedType);
                il.Emit(OpCodes.Ldarg_1);
                if (member.FieldType.IsValueType)
                    il.Emit(OpCodes.Unbox_Any, member.FieldType);
                il.Emit(OpCodes.Stfld, member);
            }

            il.Emit(OpCodes.Ret);
            return (Action<object, object>)setter.CreateDelegate(typeof(Action<object, object>));
        }

        public static Action<object, object> CreateSetter(PropertyInfo member)
        {
            var setter = new DynamicMethod(
                $"{member.Name}_set_{Guid.NewGuid():N}", 
                null, 
                new[] { typeof(object), typeof(object) }, 
                member.Module, 
                true);

            var il = setter.GetILGenerator();

            if (!member.CanWrite)
            {
                /* intentionally left blank */
            }
            else if (member.GetAccessors(true).Any(x => x.IsStatic))
            {
                il.Emit(OpCodes.Ldarg_1);
                if (member.PropertyType.IsValueType)
                    il.Emit(OpCodes.Unbox_Any, member.PropertyType);
                il.Emit(OpCodes.Call, member.SetMethod);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                if (member.ReflectedType.IsValueType)
                    il.Emit(OpCodes.Unbox, member.ReflectedType);
                il.Emit(OpCodes.Ldarg_1);
                if (member.PropertyType.IsValueType)
                    il.Emit(OpCodes.Unbox_Any, member.PropertyType);
                il.Emit(member.ReflectedType.IsValueType ? OpCodes.Call : OpCodes.Callvirt, member.SetMethod);
            }

            il.Emit(OpCodes.Ret);
            return (Action<object, object>)setter.CreateDelegate(typeof(Action<object, object>));
        }

        public static T CreateCall<T>(ConstructorInfo info)
        {
            var parameters = info.GetParameters();
            var method = new DynamicMethod(
                $"{info.ReflectedType.Name}_.ctor_{Guid.NewGuid():N}",
                typeof(object),
                Enumerable.Repeat(typeof(object), parameters.Length + 1).ToArray(), // 0th arg will be ignored - for consistent api
                info.ReflectedType.Module,
                true);

            var il = method.GetILGenerator();

            for (var i = 0; i < parameters.Length; ++i)
            {
                il.Emit(OpCodes.Ldarg, i + 1);
                if (parameters[i].ParameterType.IsValueType)
                    il.Emit(OpCodes.Unbox_Any, parameters[i].ParameterType);
            }

            il.Emit(OpCodes.Newobj, info);
            if (info.DeclaringType.IsValueType)
                il.Emit(OpCodes.Box, info.DeclaringType);

            il.Emit(OpCodes.Ret);
            return (T)(object)method.CreateDelegate(typeof(T));
        }

        public static T CreateCall<T>(MethodInfo info)
        {
            var parameters = info.GetParameters();
            var method = new DynamicMethod(
                $"{info.ReflectedType.Name}_{info.Name}_{Guid.NewGuid():N}",
                typeof(object),
                Enumerable.Repeat(typeof(object), parameters.Length + 1).ToArray(),
                info.ReflectedType.Module,
                true);

            var il = method.GetILGenerator();

            if (info.IsStatic == false)
                il.Emit(OpCodes.Ldarg_0);

            for (var i = 0; i < parameters.Length; ++i)
            {
                il.Emit(OpCodes.Ldarg, i + 1);
                if (parameters[i].ParameterType.IsValueType)
                    il.Emit(OpCodes.Unbox_Any, parameters[i].ParameterType);
            }

            if (info.IsStatic || info.DeclaringType.IsValueType)
                il.Emit(OpCodes.Call, info);
            else
                il.Emit(OpCodes.Callvirt, info);

            if (info.ReturnType == typeof(void))
                il.Emit(OpCodes.Ldnull);
            else if (info.ReturnType.IsValueType)
                il.Emit(OpCodes.Box, info.ReturnType);

            il.Emit(OpCodes.Ret);
            return (T)(object)method.CreateDelegate(typeof(T));
        }
    }
}
