using System;
using System.Collections.Generic;
using Ptixed.Sql.Metadata;

namespace Ptixed.Sql
{
    public class MappingConfig
    {
        public readonly HashSet<Type> ScalarTypes = new HashSet<Type>
        {
            typeof(string),
            typeof(DateTime), typeof(DateTime?),
            typeof(bool), typeof(bool?),
            typeof(byte), typeof(byte?),
            typeof(sbyte), typeof(sbyte?),
            typeof(char), typeof(char?),
            typeof(decimal), typeof(decimal?),
            typeof(double), typeof(double?),
            typeof(float), typeof(float?),
            typeof(int), typeof(int?),
            typeof(uint), typeof(uint?),
            typeof(long), typeof(long?),
            typeof(ulong), typeof(ulong?),
            typeof(short), typeof(short?),
            typeof(ushort), typeof(ushort?),
        };

        public object ToDb(Type type, object obj)
        {
            var ret = type == null ? null : Cache.Get(type, null, ToDbImpl).Value(obj);
            return ret ?? DBNull.Value;
        }

        protected virtual Func<object, object> ToDbImpl(Type type)
        {
            return x => x;
        }

        public object FromDb(Type type, object obj)
        {
            return type == null ? obj : Cache.Get(type, null, FromDbImpl).Value(obj);
        }

        protected virtual Func<object, object> FromDbImpl(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var subtype = type.GetGenericArguments()[0];
                return x => x == null ? null : FromDb(subtype, x);
            }

            if (!type.IsEnum && typeof(IConvertible).IsAssignableFrom(type))
                return x => x == null || x.GetType() == type
                    ? x
                    : (x is IConvertible c ? c.ToType(type, null) : ((IConvertible)x.ToString()).ToType(type, null));

            return x => x;
        }

        public virtual string FormatTableName(Table table)
        {
            return table.Name;
        }
    }
}
