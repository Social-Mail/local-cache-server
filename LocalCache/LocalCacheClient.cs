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
        switch (msg.Command)
        {
            case "get":
                return bucket.Get(msg.Key);
            case "lock":
                var locked = bucket.Get(msg.Key);
                if (locked != null)
                {
                    return "locked";
                }
                return "success";
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
