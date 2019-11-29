using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Ptixed.Sql.Meta;

namespace Ptixed.Sql
{
    public static class DatabaseExtensions
    {
        public static List<T> ToList<T>(this IDatabase db, Expression<Func<FormattableString>> expression)
            => db.Query<T>(new Query(expression)).ToList();

        public static List<T> ToList<T>(this IDatabase db, Expression<Func<FormattableString>> expression, params Type[] types)
            => db.Query<T>(new Query(expression), types).ToList();

        public static T Single<T>(this IDatabase db, Expression<Func<FormattableString>> expression)
            => db.Query<T>(new Query(expression)).Single();

        public static T SingleOrDefault<T>(this IDatabase db, Expression<Func<FormattableString>> expression)
            => db.Query<T>(new Query(expression)).SingleOrDefault();

        public static T FirstOrDefault<T>(this IDatabase db, Expression<Func<FormattableString>> expression)
            => db.Query<T>(new Query(expression)).FirstOrDefault();

        public static void NonQuery(this IDatabase db, Expression<Func<FormattableString>> expression)
            => db.NonQuery(new Query(expression));
        
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

        public static void Update(this IDatabase db, params object[] entities)
        {
            if (entities.Length == 0)
                return;
            db.NonQuery(QueryHelper.Update(entities));
        }

        public static void Delete(this IDatabase db, params object[] entities)
        {
            if (entities.Length == 0)
                return;
            db.NonQuery(QueryHelper.Delete(entities));
        }

        public static void Delete<T>(this IDatabase db, params object[] ids)
        {
            if (ids.Length == 0)
                return;
            db.NonQuery(QueryHelper.Delete<T>(ids));
        }
    }
}
