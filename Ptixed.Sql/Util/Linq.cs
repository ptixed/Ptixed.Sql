using System.Collections.Generic;
using System.Linq;

namespace Ptixed.Sql.Util
{
    internal static class Linq
    {
        public static IEnumerable<T> Except<T>(this IEnumerable<T> self, T item)
            => self.Except(new[] { item });
    }
}
