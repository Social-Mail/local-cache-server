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

    public JsonMessageClient(Socket client)
    {
        this.stream = new NetworkStream(client, ownsSocket: true);
        this.reader = new StreamReader(stream, Encoding.UTF8);
    }

    public void Run(CancellationToken stoppingToken = default)
    {
        Task.Run(async () => {
            try
            {
                using var t = this;

                // this nested try catch is important to
                // log logic error in parsing as outer
                // try might gobble up parsing errors when
                // closing stream
                try
                {
                    await this.RunAsync(stoppingToken);
                } catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
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
        while ((line = await reader.ReadLineAsync(stoppingToken)) != null)
        {
            var jsonObject = this.Deserialize(line);
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

    abstract protected Task<object> OnMessage(T msg);

    private T? Deserialize(string line)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(line);
        } catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse:{line}");
            Console.Write(ex.ToString());
        }
        return default(T);
    }

    private async Task Send(object value)
    {
        await this._asyncLock.WaitAsync();
        try
        {
            var text = JsonSerializer.Serialize(value) + "\n";
            var buffer = System.Text.Encoding.UTF8.GetBytes(text);
            await stream.WriteAsync(buffer);
            await stream.FlushAsync();
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
        }
        finally
        {
            this.stream.Dispose();
        }
    }
}
