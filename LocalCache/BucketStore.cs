using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Text;

namespace LocalCache;

internal class BucketStore
{
    private readonly IMemoryCache cache;

    public BucketStore(IMemoryCache cache)
    {
        this.cache = cache;
    }

    public IMemoryCache Get(string key)
    {
        return this.cache.GetOrCreate(key, (k) => {

            var mc = new MemoryCache(new MemoryCacheOptions {
            });

            k.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);

            return mc;

        })!;
    }
}
