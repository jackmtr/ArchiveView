using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Caching;

namespace ArchiveView.CustomHelpers
{
    public class InMemoryCache : ICacheService
    {
        public T GetOrSet<T>(string cacheKey, Func<T> getItemCallback, string role) where T : class
        {
            T item = MemoryCache.Default.Get(cacheKey) as T;
            if (item == null || role == "Admin")
            {
                item = getItemCallback();
                MemoryCache.Default.Add(cacheKey, item, DateTime.Now.AddMinutes(10));
            }
            return item;
        }

        public bool removeCache(string cacheKey) {
            MemoryCache.Default.Remove(cacheKey);

            var item = MemoryCache.Default.Get(cacheKey);
            if (item != null) {
                return true;
            }
            else{
                return false;
            }
        }
    }

    interface ICacheService
    {
        T GetOrSet<T>(string cacheKey, Func<T> getItemCallback, string role) where T : class;

        bool removeCache(string cacheKey);
    }
}