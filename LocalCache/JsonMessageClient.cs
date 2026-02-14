using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace LocalCache;

public interface IJsonMessage
{
    public string? ID { get; set; }

    public string? Command { get;set; }
}

public abstract class JsonMessageClient<T> : IDisposable
    where T : IJsonMessage
{

    private readonly SemaphoreSlim _asyncLock = new SemaphoreSlim(1, 1);
    private readonly NetworkStream stream;
    private readonly StreamReader reader;
    private readonly StreamWriter writer;

    public JsonMessageClient(Socket client)
    {
        this.stream = new NetworkStream(client, ownsSocket: true);
        this.reader = new StreamReader(stream, Encoding.UTF8);
        this.writer = new StreamWriter(stream, Encoding.UTF8);
    }

    public void Run(CancellationToken stoppingToken = default)
    {
        Task.Run(async () => {
            try
            {
                using var t = this;
                await this.RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        });
    }

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        string? line;
        while (true)
        {
            while ((line = await reader.ReadLineAsync(stoppingToken)) != null)
            {
                var jsonObject = JsonSerializer.Deserialize<T>(line);
                if (jsonObject == null)
                {
                    continue;
                }
                if (jsonObject.Command == "ping")
                {
                    await this.Send(new
                    {
                        id = jsonObject.ID,
                        result = "pong"
                    });
                    continue;
                }
                try
                {
                    var value = await this.OnMessage(jsonObject);
                    await this.Send(new {
                        id = jsonObject.ID,
                        value
                    });
                } catch (Exception error)
                {
                    await this.Send(new
                    {
                        id = jsonObject.ID,
                        error = error.Message,
                        details = error.ToString()
                    });
                }
            }
        }
    }

    abstract protected Task<object> OnMessage(T msg);


    private async Task Send(object value)
    {
        await this._asyncLock.WaitAsync();
        try
        {
            await writer.WriteLineAsync(JsonSerializer.Serialize(value));
            await writer.FlushAsync();
        }
        finally
        {
            this._asyncLock.Release();
        }
    }

    public void Dispose()
    {
        try
        {
            this.reader.Dispose();
            this.writer.Dispose();
        }
        finally
        {
            this.stream.Dispose();
        }
    }
}
