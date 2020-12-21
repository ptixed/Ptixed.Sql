using Ptixed.Sql.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ptixed.Sql
{
    public static class Extensions
    {
        #region ToList with Query

        public static T Single<T>(this IDatabase db, Query query)
            => db.Query<T>(query).Single();
        public static T SingleOrDefault<T>(this IDatabase db, Query query)
            => db.Query<T>(query).SingleOrDefault();
        public static List<T> ToList<T>(this IDatabase db, Query query, params Type[] types)
            => db.Query<T>(query, types).ToList();

        public static List<T1> ToList<T1>(this IDatabase db, Query query)
            => ToList<T1>(db, query, new[] { typeof(T1) });
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

        #region ToList with FormattableString

        public static void NonQuery(this IDatabase db, FormattableString query)
            => db.NonQuery(new Query(query));

        public static T Single<T>(this IDatabase db, FormattableString query)
            => db.Query<T>(new Query(query)).Single();
        public static T SingleOrDefault<T>(this IDatabase db, FormattableString query)
            => db.Query<T>(new Query(query)).SingleOrDefault();
        public static List<T> ToList<T>(this IDatabase db, FormattableString query, params Type[] types)
            => db.Query<T>(new Query(query), types).ToList();

        public static List<T1> ToList<T1>(this IDatabase db, FormattableString query)
            => ToList<T1>(db, query, new[] { typeof(T1) });
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

        #region IQueryExecutor with sinigle paramater item

        public static void NonQuery(this IDatabase db, Query query)
        {
            db.NonQuery(new[] { query });
        }

        public static T GetById<T>(this IDatabase db, object id)
        {
            return db.GetById<T>(new[] { id }).SingleOrDefault();
        }

        public static T Insert<T>(this IDatabase db, T entity)
        {
            db.Insert(new[] { entity });
            return entity;
        }

        public static void Update(this IDatabase db, object entity)
        {
            db.Update(new[] { entity });
        }

        public static void Delete(this IDatabase db, Type type, object id)
        {
            db.Delete(new (Table, object)[] { (type, id) });
        }

        #endregion

        #region Delete alternatives

        public static void Delete<T>(this IDatabase db, IEnumerable<object> ids)
        {
            var table = Table.Get(typeof(T));
            db.Delete(ids.Select(x => (table, x)));
        }

        public static void Delete<T>(this IDatabase db, object id)
        {
            var table = Table.Get(typeof(T));
            db.Delete(new[] { (table, id) });
        }

        public static void Delete<T>(this IDatabase db, IEnumerable<T> entities)
        {
            var table = Table.Get(typeof(T));
            db.Delete(entities.Select(x => (table, table[x, table.PrimaryKey])));
        }

        public static void Delete<T>(this IDatabase db, T entity)
        {
            var table = Table.Get(typeof(T));
            db.Delete(new[] { (table, table[entity, table.PrimaryKey]) });
        }

        #endregion
    }
}
