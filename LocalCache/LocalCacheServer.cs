using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace LocalCache;

internal class LocalCacheServer : BackgroundService
{
    private readonly IMemoryCache cache;

    public LocalCacheServer(IMemoryCache cache)
    {
        this.cache = cache;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // since this server will always be local
        // this has to be a unix path
        var port = System.Environment.GetEnvironmentVariable("PORT");
        if(string.IsNullOrEmpty(port) )
        {
            throw new ArgumentNullException(nameof(port));
        }

        while (!stoppingToken.IsCancellationRequested)
        {

            if (File.Exists(port))
            {
                File.Delete(port);
            }
            var endPoint = new UnixDomainSocketEndPoint(port);
            using var serverSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            serverSocket.Bind(endPoint);
            serverSocket.Listen();

            Console.WriteLine($"Cache server started on {port}");

            while (!stoppingToken.IsCancellationRequested)
            {
                var client = await serverSocket.AcceptAsync();
                _ = Task.Run(async () => {
                    try
                    {
                        await this.ProcessClient(client, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                });
            }
        }

    }

    async Task ProcessClient(Socket client, CancellationToken stoppingToken)
    {
        using var stream = new NetworkStream(client, ownsSocket: true);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        using var writer = new StreamWriter(stream, Encoding.UTF8);
        string? line;
        object? value;
        while (true)
        {
            while ((line = await reader.ReadLineAsync(stoppingToken)) != null)
            {
                var jsonObject = JsonSerializer.Deserialize<CacheMessage>(line);
                if(jsonObject == null)
                {
                    continue;
                }
                switch(jsonObject.Command) {
                    case "ping":
                        await writer.WriteLineAsync(JsonSerializer.Serialize(new {
                            id = jsonObject.ID,
                            result = "pong"
                        }));
                        await writer.FlushAsync();
                        break;
                    case "get":
                        if(string.IsNullOrWhiteSpace(jsonObject.Key))
                        {
                            await writer.WriteLineAsync(JsonSerializer.Serialize(new
                            {
                                id = jsonObject.ID,
                                error = "key-empty"
                            }));
                            await writer.FlushAsync();
                            continue;
                        }
                        value = this.cache.Get(jsonObject.Key);
                        await writer.WriteLineAsync(JsonSerializer.Serialize(new
                        {
                            id = jsonObject.ID,
                            value
                        }));
                        await writer.FlushAsync();
                        break;
                    case "set":
                        if (string.IsNullOrWhiteSpace(jsonObject.Key))
                        {
                            await writer.WriteLineAsync(JsonSerializer.Serialize(new
                            {
                                id = jsonObject.ID,
                                error = "key-empty"
                            }));
                            await writer.FlushAsync();
                            continue;
                        }
                        value = jsonObject.Value;
                        var maxAge = jsonObject.MaxAge ?? 4;
                        var ttl = jsonObject.TTL ?? 15;
                        this.cache.Set(jsonObject.Key, value, new MemoryCacheEntryOptions {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(maxAge),
                            SlidingExpiration = TimeSpan.FromSeconds(ttl),
                        });
                        await writer.WriteLineAsync(JsonSerializer.Serialize(new
                        {
                            id = jsonObject.ID,
                            value
                        }));
                        await writer.FlushAsync();
                        break;
                    case "delete":
                        if (string.IsNullOrWhiteSpace(jsonObject.Key))
                        {
                            await writer.WriteLineAsync(JsonSerializer.Serialize(new
                            {
                                id = jsonObject.ID,
                                error = "key-empty"
                            }));
                            await writer.FlushAsync();
                            continue;
                        }
                        this.cache.Remove(jsonObject.Key);
                        await writer.WriteLineAsync(JsonSerializer.Serialize(new
                        {
                            id = jsonObject.ID,
                            result="removed"
                        }));
                        await writer.FlushAsync();
                        break;
                }
            }
        }
    }
}
