using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Ptixed.Sql.Impl
{
    public class Accessor<TKey>
    {
        private readonly Dictionary<TKey, Func<object, object>> _getters;
        private readonly Dictionary<TKey, Action<object, object>> _setters;

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
            Type = type;

            var members = lookup.Where(x => !(x.Value is MethodBase));

            _setters = members.ToDictionary(x => x.Key, x => Accessor.CreateSetter(type, x.Value));
            _getters = members.ToDictionary(x => x.Key, x => Accessor.CreateGetter(type, x.Value));

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
                }
        }

        public T Invoke<T>(object target, TKey name) => (T)_methods0[name](target);
        public T Invoke<T>(object target, TKey name, object p1) => (T)_methods1[name](target, p1);
        public T Invoke<T>(object target, TKey name, object p1, object p2) => (T)_methods2[name](target, p1, p2);
        public T Invoke<T>(object target, TKey name, object p1, object p2, object p3) => (T)_methods3[name](target, p1, p2, p3);
        public T Invoke<T>(object target, TKey name, object p1, object p2, object p3, object p4) => (T)_methods4[name](target, p1, p2, p3, p4);
    }

    public class Accessor
    {
        public static Func<object, object> CreateGetter(Type type, MemberInfo member)
        {
            var self = OpCodes.Ldarg_0;

            var getter = new DynamicMethod(type.FullName + "_get", typeof(object), new[]
                {
                    typeof(object)
                }, type.Module, true);
            var il = getter.GetILGenerator();

            il.Emit(self);
            if (type.IsValueType)
                il.Emit(OpCodes.Unbox, type);

            switch (member)
            {
                case PropertyInfo pi: // when pi.CanRead:
                    il.Emit(type.IsValueType ? OpCodes.Call : OpCodes.Callvirt, pi.GetMethod);
                    if (pi.PropertyType.IsValueType)
                        il.Emit(OpCodes.Box, pi.PropertyType);
                    break;
                case FieldInfo fi:
                    il.Emit(OpCodes.Ldfld, fi);
                    if (fi.FieldType.IsValueType)
                        il.Emit(OpCodes.Box, fi.FieldType);
                    break;
                default:
                    throw PtixedException.InvalidExpression(member);
            }
            il.Emit(OpCodes.Ret);

            return (Func<object, object>)getter.CreateDelegate(typeof(Func<object, object>));
        }

        public static Action<object, object> CreateSetter(Type type, MemberInfo member)
        {
            var self = OpCodes.Ldarg_0;
            var value = OpCodes.Ldarg_1;

            var setter = new DynamicMethod(type.FullName + "_set", null, new[]
                {
                    typeof(object),
                    typeof(object),
                }, type.Module, true);
            var il = setter.GetILGenerator();

            il.Emit(self);
            if (type.IsValueType)
                il.Emit(OpCodes.Unbox, type);

            switch (member)
            {
                case PropertyInfo pi when pi.CanWrite:
                    il.Emit(value);
                    il.Emit(pi.PropertyType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, pi.PropertyType);
                    il.Emit(type.IsValueType ? OpCodes.Call : OpCodes.Callvirt, pi.SetMethod);
                    break;
                case PropertyInfo pi:
                    il.Emit(OpCodes.Pop);
                    break;
                case FieldInfo fi:
                    il.Emit(value);
                    il.Emit(fi.FieldType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, fi.FieldType);
                    il.Emit(OpCodes.Stfld, fi);
                    break;
                default:
                    throw PtixedException.InvalidExpression(member);
            }

            il.Emit(OpCodes.Ret);

            return (Action<object, object>)setter.CreateDelegate(typeof(Action<object, object>));
        }

        public static T CreateCall<T>(ConstructorInfo info)
        {
            var parameters = info.GetParameters();
            var method = new DynamicMethod(
                info.ReflectedType.FullName + "_new",
                typeof(object),
                Enumerable.Repeat(typeof(object), parameters.Length + 1).ToArray(), // 0th arg will be ignored - for consistent api
                info.ReflectedType.Module,
                true);

            var il = method.GetILGenerator();

            for (var i = 0; i < parameters.Length; ++i)
            {
                il.Emit(OpCodes.Ldarg, i + 1);
                il.Emit(parameters[i].ParameterType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, parameters[i].ParameterType);
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
                info.ReflectedType.FullName + "_" + info.Name,
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
                il.Emit(parameters[i].ParameterType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, parameters[i].ParameterType);
            }

            if (info.IsStatic || info.DeclaringType.IsValueType)
                il.Emit(OpCodes.Call, info);
            else
                il.Emit(OpCodes.Callvirt, info);

            if (info.ReturnType == typeof(void))
                il.Emit(OpCodes.Ldnull); // return anything of info returns void
            else if (info.ReturnType.IsValueType)
                il.Emit(OpCodes.Box, info.ReturnType);

            il.Emit(OpCodes.Ret);

            return (T)(object)method.CreateDelegate(typeof(T));
        }
    }
}
