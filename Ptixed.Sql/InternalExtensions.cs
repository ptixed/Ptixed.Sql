using System.Collections.Generic;

namespace Ptixed.Sql
{
    internal static class InternalExtensions
    {
        internal static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> tuple, out T1 key, out T2 value)
        {
            key = tuple.Key;
            value = tuple.Value;
        }
    }
}
