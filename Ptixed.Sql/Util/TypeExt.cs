using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ptixed.Sql.Util
{
    public static class TypeExt
    {
        public static bool IsAssignableFrom(this Type self, Type type)
        {
            return self.GetTypeInfo().IsAssignableFrom(type.GetTypeInfo());
        }

        public static bool IsEnum(this Type self)
        {
            return self.GetTypeInfo().IsEnum;
        }

        public static bool IsValueType(this Type self)
        {
            return self.GetTypeInfo().IsValueType;
        }

        public static ConstructorInfo GetConstructor(this Type self, Type[] types)
        {
            return self.GetTypeInfo().DeclaredConstructors
                .FirstOrDefault(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(types));
        }

        public static T GetCustomAttribute<T>(this Type self)
        {
            return self.GetTypeInfo().CustomAttributes.OfType<T>().SingleOrDefault();
        }

        public static List<FieldInfo> GetFields(this Type self)
        {
            var ret = new List<FieldInfo>();
            for (var t = self.GetTypeInfo(); t != null; t = t.BaseType?.GetTypeInfo())
                ret.AddRange(t.DeclaredFields.Where(x => !x.IsStatic));
            return ret;
        }

        public static Type[] GetGenericArguments(this Type self)
        {
            return self.GenericTypeArguments;
        }

        public static List<Type> GetInterfaces(this Type self)
        {
            return self.GetTypeInfo().ImplementedInterfaces.ToList();
        }

        public static MethodInfo GetMethod(this Type self, string name)
        {
            for (var t = self.GetTypeInfo(); t != null; t = t.BaseType?.GetTypeInfo())
            {
                var method = t.GetDeclaredMethod(name);
                if (method != null)
                    return method;
            }
            return null;
        }

        public static MethodInfo GetMethod(this Type self, string name, Type[] types)
        {
            for (var t = self.GetTypeInfo(); t != null; t = t.BaseType?.GetTypeInfo())
            {
                var method = t.GetDeclaredMethods(name).FirstOrDefault(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(types));
                if (method != null)
                    return method;
            }
            return null;
        }

        public static Module GetModule(this Type self)
        {
            return self.GetTypeInfo().Module;
        }

        public static List<PropertyInfo> GetProperties(this Type self)
        {
            var ret = new List<PropertyInfo>();
            for (var t = self.GetTypeInfo(); t != null; t = t.BaseType?.GetTypeInfo())
                ret.AddRange(t.DeclaredProperties.Where(x => x.GetMethod?.IsStatic != true && x.SetMethod?.IsStatic != true));
            return ret;
        }
    }
}
