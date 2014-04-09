﻿using System;
using System.Collections.Generic;
using NzbDrone.Common.EnsureThat;

namespace NzbDrone.Common.Cache
{
    public interface ICacheManager
    {
        ICached<T> GetCache<T>(Type host, string name);
        ICached<T> GetCache<T>(Type host);
        void Clear();
        ICollection<ICached> Caches { get; }
    }

    public class CacheManager : ICacheManager
    {
        private readonly ICached<ICached> _cache;

        public CacheManager()
        {
            _cache = new Cached<ICached>();

        }

        public ICached<T> GetCache<T>(Type host)
        {
            Ensure.That(host, () => host).IsNotNull();
            return GetCache<T>(host, host.FullName);
        }

        public void Clear()
        {
            _cache.Clear();
        }

        public ICollection<ICached> Caches { get { return _cache.Values; } }

        public ICached<T> GetCache<T>(Type host, string name)
        {
            Ensure.That(host, () => host).IsNotNull();
            Ensure.That(name, () => name).IsNotNullOrWhiteSpace();

            return (ICached<T>)_cache.Get(host.FullName + "_" + name, () => new Cached<T>());
        }
    }
}