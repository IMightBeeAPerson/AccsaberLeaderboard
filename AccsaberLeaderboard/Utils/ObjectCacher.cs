using System;
using System.Collections.Generic;
using System.Linq;

namespace AccsaberLeaderboard.Utils
{
    public class ObjectCacher<K, V>(TimeSpan defaultItemLifespan = default) where K : IComparable<K>
    {
#nullable enable
        private readonly Dictionary<K, V> cache = [];
        private readonly SortedSet<(DateTime expiration, K key)> expirationDates = [];
        private readonly TimeSpan itemLifespan = defaultItemLifespan == default ? new(0, 5, 0) : defaultItemLifespan;

        public void CacheItem(V item, TimeSpan lifespan = default)
        {
            CacheItem(item, GetKey(item), lifespan);
        }
        public void CacheItem(V item, K key, TimeSpan lifespan = default)
        {
            CleanOutCache();
            cache[key] = item;
            if (lifespan == default)
                lifespan = itemLifespan;
            expirationDates.Add((DateTime.Now + lifespan, key));
        }

        public void CacheItemPermanently(V item) => CacheItemPermanently(item, GetKey(item));
        public void CacheItemPermanently(V item, K key)
        {
            CleanOutCache();
            cache[key] = item;
        }

        public V? GetCachedItem(K key)
        {
            CleanOutCache();
            if (!cache.TryGetValue(key, out V val))
                return default;
            return val;
        }
        public bool TryGetCachedItem(K key, out V? val)
        {
            CleanOutCache();
            return cache.TryGetValue(key, out val);
        }

        public void RemoveItem(K key)
        {
            if (cache.ContainsKey(key))
            {
                V item = cache[key];
                cache.Remove(key);
                expirationDates.Remove(expirationDates.First(token => token.key.Equals(key)));
            }
        }
        public void ClearCache()
        {
            cache.Clear();
            expirationDates.Clear();
        }

        private void CleanOutCache()
        {
            DateTime rn = DateTime.Now;
            while (expirationDates.Count > 0 && expirationDates.Min.expiration < rn)
            {
                cache.Remove(expirationDates.Min.key);
                expirationDates.Remove(expirationDates.Min);
            }
        }
        private K GetKey(V item)
        {
            if (item is ICacheKey keyHolder)
                return keyHolder.Key;
            throw new Exception("Cannot get the key of a type that doesn't implement ICacheKey.");
        }

        public interface ICacheKey
        {
            public K Key { get; }
        }
    }

    public class ObjectCacher<T> : ObjectCacher<string, T> { }
}
