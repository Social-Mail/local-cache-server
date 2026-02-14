using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LocalCache;

internal class LocalCacheServer : BackgroundService
{
    private readonly BucketStore store;

    public LocalCacheServer(BucketStore store)
    {
        this.store = store;
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
                var cacheClient = new LocalCacheClient(this.store, client);
                cacheClient.Run();
            }
        }

    }

}
