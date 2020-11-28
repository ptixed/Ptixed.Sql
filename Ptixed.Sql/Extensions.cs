using Ptixed.Sql.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ptixed.Sql
{
    public static class Extensions
    {
        #region Query

        public static T Single<T>(this IDatabase db, Query query)
            => db.Query<T>(query).Single();
        public static T SingleOrDefault<T>(this IDatabase db, Query query)
            => db.Query<T>(query).SingleOrDefault();
        public static List<T> ToList<T>(this IDatabase db, Query query, params Type[] types)
            => db.Query<T>(query, types).ToList();

        public static List<T> ToList<T>(this IDatabase db, Query query)
            => ToList<T>(db, query, new[] { typeof(T) });
        public static List<T1> ToList<T1, T2>(this IDatabase db, Query query)
            => ToList<T1>(db, query, new[] { typeof(T1), typeof(T2) });
        public static List<T1> ToList<T1, T2, T3>(this IDatabase db, Query query)
            => ToList<T1>(db, query, new[] { typeof(T1), typeof(T2), typeof(T3) });
        public static List<T1> ToList<T1, T2, T3, T4>(this IDatabase db, Query query)
            => ToList<T1>(db, query, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) });
        public static List<T1> ToList<T1, T2, T3, T4, T5>(this IDatabase db, Query query)
            => ToList<T1>(db, query, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) });
        public static List<T1> ToList<T1, T2, T3, T4, T5, T6>(this IDatabase db, Query query)
            => ToList<T1>(db, query, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6) });
        public static List<T1> ToList<T1, T2, T3, T4, T5, T6, T7>(this IDatabase db, Query query)
            => ToList<T1>(db, query, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7) });
        public static List<T1> ToList<T1, T2, T3, T4, T5, T6, T7, T8>(this IDatabase db, Query query)
            => ToList<T1>(db, query, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8) });

        #endregion

        #region FormattableString

        public static void NonQuery(this IDatabase db, FormattableString query)
            => db.NonQuery(new Query(query));

        public static T Single<T>(this IDatabase db, FormattableString query)
            => db.Query<T>(new Query(query)).Single();
        public static T SingleOrDefault<T>(this IDatabase db, FormattableString query)
            => db.Query<T>(new Query(query)).SingleOrDefault();
        public static List<T> ToList<T>(this IDatabase db, FormattableString query, params Type[] types)
            => db.Query<T>(new Query(query), types).ToList();

        public static List<T> ToList<T>(this IDatabase db, FormattableString query)
            => ToList<T>(db, query, new[] { typeof(T) });
        public static List<T1> ToList<T1, T2>(this IDatabase db, FormattableString query)
            => ToList<T1>(db, query, new[] { typeof(T1), typeof(T2) });
        public static List<T1> ToList<T1, T2, T3>(this IDatabase db, FormattableString query)
            => ToList<T1>(db, query, new[] { typeof(T1), typeof(T2), typeof(T3) });
        public static List<T1> ToList<T1, T2, T3, T4>(this IDatabase db, FormattableString query)
            => ToList<T1>(db, query, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) });
        public static List<T1> ToList<T1, T2, T3, T4, T5>(this IDatabase db, FormattableString query)
            => ToList<T1>(db, query, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) });
        public static List<T1> ToList<T1, T2, T3, T4, T5, T6>(this IDatabase db, FormattableString query)
            => ToList<T1>(db, query, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6) });
        public static List<T1> ToList<T1, T2, T3, T4, T5, T6, T7>(this IDatabase db, FormattableString query)
            => ToList<T1>(db, query, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7) });
        public static List<T1> ToList<T1, T2, T3, T4, T5, T6, T7, T8>(this IDatabase db, FormattableString query)
            => ToList<T1>(db, query, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8) });

        #endregion

        public static List<T> GetByIds<T>(this IDatabase db, params object[] ids)
        {
            if (ids.Length == 0)
                return new List<T>();
            return db.Query<T>(QueryBuilder.GetById<T>(ids)).ToList();
        }

        public static T GetById<T>(this IDatabase db, object id)
        {
            return GetByIds<T>(db, id).SingleOrDefault();
        }

        internal static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> tuple, out T1 key, out T2 value)
        {
            key = tuple.Key;
            value = tuple.Value;
        }
    }
}
