using Ptixed.Sql.Impl;
using System;
using System.Reflection;

namespace Ptixed.Sql.Util
{
    public static class Reflection
    {
        public static Type GetMemberType(this MemberInfo member)
        {
            switch (member)
            {
                case FieldInfo fi:
                    return fi.FieldType;
                case PropertyInfo pi:
                    return pi.PropertyType;
                default:
                    throw PtixedException.InvalidExpression(member);
            }
        }
    }
}
