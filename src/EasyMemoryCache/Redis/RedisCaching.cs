﻿using AsyncKeyedLock;
using EasyMemoryCache.Accessors;
using EasyMemoryCache.Configuration;
using EasyMemoryCache.Extensions;
using StackExchange.Redis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EasyMemoryCache.Redis
{
    public class RedisCaching : ICaching, IDisposable
    {
        private readonly CacheAccessor _cacheAccessor;
        private readonly IServer _server;

        private readonly AsyncKeyedLocker<string> _cacheLock = new AsyncKeyedLocker<string>(o =>
        {
            o.PoolSize = 20;
            o.PoolInitialFill = 1;
        });

        public RedisCaching(CacheAccessor cacheAccessor, IServer server)
        {
            _cacheAccessor = cacheAccessor;
            _server = server;
        }

        public T GetOrSetObjectFromCache<T>(string cacheItemName, int cacheTime, Func<T> objectSettingFunction, bool cacheEmptyList = false, CacheTimeInterval interval = CacheTimeInterval.Minutes)
        {
            T cachedObject = _cacheAccessor.Get<T>(cacheItemName);

            if (cachedObject == null || EqualityComparer<T>.Default.Equals(cachedObject, default))
            {
                using (_cacheLock.Lock(cacheItemName))
                {
                    try
                    {
                        cachedObject = objectSettingFunction();
                        var oType = cachedObject.GetType();
                        if (oType.IsGenericType && oType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            if (((ICollection)cachedObject).Count > 0 || cacheEmptyList)
                            {
                                _cacheAccessor.Set(cacheItemName, cachedObject, ConvertInterval.Convert(interval, cacheTime));
                            }
                        }
                        else
                        {
                            _cacheAccessor.Set(cacheItemName, cachedObject, ConvertInterval.Convert(interval, cacheTime));
                        }
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine(err.Message);
                        return cachedObject;
                    }
                }
            }
            return cachedObject;
        }

        public async Task<T> GetOrSetObjectFromCacheAsync<T>(string cacheItemName, int cacheTime, Func<Task<T>> objectSettingFunction, bool cacheEmptyList = false, CacheTimeInterval interval = CacheTimeInterval.Minutes)
        {
            T cachedObject = await _cacheAccessor.GetAsync<T>(cacheItemName).ConfigureAwait(false);

            if (cachedObject == null || EqualityComparer<T>.Default.Equals(cachedObject, default))
            {
                using (await _cacheLock.LockAsync(cacheItemName).ConfigureAwait(false))
                {
                    try
                    {
                        cachedObject = await objectSettingFunction().ConfigureAwait(false);

                        var oType = cachedObject.GetType();
                        if (oType.IsGenericType && oType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            if (((ICollection)cachedObject).Count > 0 || cacheEmptyList)
                            {
                                await _cacheAccessor.SetAsync(cacheItemName, cachedObject, ConvertInterval.Convert(interval, cacheTime)).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            await _cacheAccessor.SetAsync(cacheItemName, cachedObject, ConvertInterval.Convert(interval, cacheTime)).ConfigureAwait(false);
                        }
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine(err.Message);
                        return cachedObject;
                    }
                }
            }
            return cachedObject;
        }

        public void Invalidate(string key)
        {
            _cacheAccessor.Remove(key);
        }

        public void InvalidateAll()
        {
            _server.FlushDatabase();
        }

        public async Task InvalidateAllAsync()
        {
            await _server.FlushDatabaseAsync();
        }

        public void SetValueToCache(string key, object value, int cacheTime = 120, CacheTimeInterval interval = CacheTimeInterval.Minutes)
        {
            _cacheAccessor.Set(key, value, ConvertInterval.Convert(interval, cacheTime));
        }

        public async Task SetValueToCacheAsync(string key, object value, int cacheTime = 120, CacheTimeInterval interval = CacheTimeInterval.Minutes)
        {
            await _cacheAccessor.SetAsync(key, value, ConvertInterval.Convert(interval, cacheTime));
        }

        public object GetValueFromCache(string key)
        {
            return _cacheAccessor.Get(key);
        }

        public async Task<T> GetValueFromCacheAsync<T>(string key)
        {
            return await _cacheAccessor.GetAsync<T>(key);
        }

        public T GetValueFromCache<T>(string key)
        {
            return _cacheAccessor.Get<T>(key);
        }

        public void Dispose()
        {
            _cacheAccessor.Dispose();
        }

        public IEnumerable<string> GetKeys()
        {
            return _server.Keys().Select(x => x.ToString());
        }
    }
}