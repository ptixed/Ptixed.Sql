using System;
using System.Collections.Generic;
using System.Linq;
using Ptixed.Sql.Meta;
using Ptixed.Sql.SqlServer;

namespace Ptixed.Sql.SqlServer
{
    public static class DatabaseExtensions
    {
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

        public static T Single<T>(this IDatabase db, FormattableString query)
            => db.Query<T>(new Query(query)).Single();

        public static T SingleOrDefault<T>(this IDatabase db, FormattableString query)
            => db.Query<T>(new Query(query)).SingleOrDefault();

        public static T FirstOrDefault<T>(this IDatabase db, FormattableString query)
            => db.Query<T>(new Query(query)).FirstOrDefault();

        public static int NonQuery(this IDatabase db, FormattableString query)
            => db.NonQuery(new Query(query));

        public static T Upsert<T>(this IDatabase db, FormattableString searchCondition, T obj)
            => db.Query<T>(QueryHelper.Upsert(new Query(searchCondition), obj)).Single();

        public static List<T> GetByIds<T>(this IDatabase db, params object[] ids)
        {
            if (ids.Length == 0)
                return new List<T>();
            return db.Query<T>(QueryHelper.GetById<T>(ids)).ToList();
        }

        public static T GetById<T>(this IDatabase db, object id)
        {
            return GetByIds<T>(db, id).SingleOrDefault();
        }

        public static List<T> Insert<T>(this IDatabase db, params T[] entities)
        {
            if (entities.Length == 0)
                return new List<T>();
            var result = db.Query<T>(QueryHelper.Insert(entities)).ToList();

            var table = Table.Get(typeof(T));

            if (table.AutoIncrementColumn != null)
                foreach (var (inserted, i) in result.Select((x, i) => (x, i)))
                    table[entities[i], table.AutoIncrementColumn.LogicalColumn] = table[inserted, table.AutoIncrementColumn.LogicalColumn];

            return entities.ToList();
        }

        public static T Insert<T>(this IDatabase db, T entity)
        {
            return Insert(db, new [] { entity })[0];
        }

        public static string Insert(this IDatabase db, string table, IDictionary<string, object> values)
        {
            return db.Query<string>(QueryHelper.Insert(table, values)).Single();
        }

        public static int Update(this IDatabase db, params object[] entities)
        {
            if (entities.Length == 0)
                return 0;
            return db.NonQuery(QueryHelper.Update(entities));
        }

        public static int Delete(this IDatabase db, params object[] entities)
        {
            if (entities.Length == 0)
                return 0;
            return db.NonQuery(QueryHelper.Delete(entities));
        }

        public static int Delete<T>(this IDatabase db, params object[] ids)
        {
            if (ids.Length == 0)
                return 0;
            return db.NonQuery(QueryHelper.Delete<T>(ids));
        }
    }
}
