﻿using System;
using System.Threading.Tasks;

namespace EasyMemoryCache
{
    public interface ICaching
    {
        T GetOrSetObjectFromCache<T>(string cacheItemName, int cacheTimeInMinutes, Func<T> objectSettingFunction);

        Task<T> GetOrSetObjectFromCacheAsync<T>(string cacheItemName, int cacheTimeInMinutes, Func<T> objectSettingFunction);

        void Invalidate(string key);

        void InvalidateAll();

        void SetValueToCache(string key, object value, int cacheTimeInMinutes = 120);

        object GetValueFromCache(string key);
    }
}