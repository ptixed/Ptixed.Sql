using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;

namespace Ptixed.Sql
{
    public class Cached<T>
    {
        private T _value;
        public T Value
        {
            get => _value;
            set
            {
                _value = value;
                ETag++;
                CreatedAt = DateTime.Now;
                IsExpired = false;
            }
        }

        public bool HasValue => ETag != 0;
        public bool IsExpired { get; set; }
        public DateTime CreatedAt { get; private set; }
        public int ETag { get; private set; }

        internal bool IsDecayed(Func<T, TimeSpan> decay)
        {
            return !HasValue
                || IsExpired
                || (decay != null && CreatedAt.Add(decay(Value)) < DateTime.Now);
        }
    }

    public static class Cache
    {
        internal static class CacheStore<TIn, TOut>
        {
            public static readonly ConcurrentDictionary<TIn, TOut> Store = new ConcurrentDictionary<TIn, TOut>();
        }

        /// <summary>
        /// Cache is refreshed, other requests will wait for refresh to be completed
        /// </summary>
        public static Cached<TOut> Get<TIn, TOut>(TIn arg, Func<TOut, TimeSpan> decay, Func<TIn, TOut> factory)
        {
            var item = CacheStore<(MethodInfo, TIn), Cached<TOut>>.Store.GetOrAdd((factory.Method, arg), _ => new Cached<TOut>());
            var etag = item.ETag;

            if (!item.IsDecayed(decay))
                return item;

            lock (item)
                if (etag == item.ETag)
                    item.Value = factory(arg);

            return item;
        }

        /// <summary>
        /// Cache is refreshed while other requests can grab old value
        /// </summary>
        public static Cached<TOut> GetWithLazyRefresh<TIn, TOut>(TIn arg, Func<TOut, TimeSpan> decay, Func<TIn, TOut> factory)
        {
            var item = CacheStore<(MethodInfo, TIn), Cached<TOut>>.Store.GetOrAdd((factory.Method, arg), _ => new Cached<TOut>());
            var etag = item.ETag;

            if (!item.IsDecayed(decay))
                return item;

            var locked = Monitor.TryEnter(item, TimeSpan.Zero);
            if (!locked && !item.HasValue)
            {
                Monitor.Enter(item);
                locked = true;
            }

            if (locked)
                try
                {
                    if (etag == item.ETag)
                        item.Value = factory(arg);
                }
                finally
                {
                    Monitor.Exit(item);
                }

            return item;
        }

        public static TOut RunOnce<TOut>(Func<TOut> factory)
        {
            return Get(-1, null, _ => factory()).Value;
        }
    }
}
