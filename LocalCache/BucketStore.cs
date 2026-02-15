using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Text;

namespace LocalCache;

public class Bucket: IDisposable
{
    public readonly string BucketName;

    public readonly IMemoryCache Cache;

    public readonly IMemoryCache Locks;

    public Bucket(string name)
    {
        this.BucketName = name;
        this.Cache = new MemoryCache(new MemoryCacheOptions { });
        this.Locks= new MemoryCache(new MemoryCacheOptions { });
    }

    public void Dispose()
    {
        this.Cache.Dispose();
        this.Locks.Dispose();
    }
}

internal class BucketStore
{
    private readonly IMemoryCache cache;

    public BucketStore(IMemoryCache cache)
    {
        this.cache = cache;
    }

    public Bucket Get(string key)
    {
        return this.cache.GetOrCreate(key, (k) => {

            k.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);

            return new Bucket(key);

        })!;
    }

    internal void Clear(Bucket bucket)
    {
        Task.Run(() =>
        {
            try
            {
                this.cache.Remove(bucket.BucketName);
                bucket.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        });
    }
}
