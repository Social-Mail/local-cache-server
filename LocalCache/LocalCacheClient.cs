using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LocalCache;

internal class LocalCacheClient: JsonMessageClient<CacheMessage>
{
    public long lockID = 1;

    private readonly BucketStore store;

    public LocalCacheClient(BucketStore store, Socket client): base(client)
    {
        this.store = store;
    }

    protected async override Task<object?> OnMessage(CacheMessage msg)
    {
        if (string.IsNullOrWhiteSpace(msg.Bucket))
        {
            throw new ArgumentException("Bucket is required");
        }
        if (string.IsNullOrWhiteSpace(msg.Key))
        {
            throw new ArgumentException("Key is required");
        }

        var bucket = this.store.Get(msg.Bucket);
        object? value;
        string n;
        string? locked;
        switch (msg.Command)
        {
            case "get":
                return bucket.Get(msg.Key);
            case "renew-lock":
                if (msg.Value == null)
                {
                    throw new ArgumentException("Invalid lock id");
                }
                n = msg.Value.AsValue().ToString();
                locked = bucket.GetOrCreate<string>(msg.Key, (x) =>
                {
                    x.SlidingExpiration = TimeSpan.FromSeconds(15);
                    return n;
                });
                if (locked == n)
                {
                    return n.ToString();
                }
                return null;
            case "lock":
                n = Interlocked.Increment(ref this.lockID).ToString();
                locked = bucket.GetOrCreate<string>(msg.Key, (x) =>
                {
                    x.SlidingExpiration = TimeSpan.FromSeconds(15);
                    return n;
                });
                if (locked == n)
                {
                    return n.ToString();
                }
                return null;
            case "set":
                value = msg.Value;
                var maxAge = msg.MaxAge ?? 4;
                var ttl = msg.TTL ?? 15;
                bucket.Set(msg.Key, value, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(maxAge),
                    SlidingExpiration = TimeSpan.FromSeconds(ttl),
                });
                return "done";
            case "delete":
                bucket.Remove(msg.Key);
                return "removed";
            case "clear":
                this.store.Clear(bucket, msg.Bucket);
                return "done";
        }
        return "unknown";
    }
}
