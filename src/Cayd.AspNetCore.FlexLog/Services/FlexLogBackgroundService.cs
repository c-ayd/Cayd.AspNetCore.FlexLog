using Cayd.AspNetCore.FlexLog.Logging;
using Cayd.AspNetCore.FlexLog.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text.Json;
using System;

namespace Cayd.AspNetCore.FlexLog.Services
{
    public class FlexLogBackgroundService : BackgroundService
    {
        private static readonly string _invalidJsonText = "INVALID JSON";
        private static readonly string _redactedText = "REDACTED";

        private readonly FlexLogChannel _logChannel;

        private readonly int _bufferLimit;
        private readonly int _timer;
        private readonly HashSet<string> _redactedKeysFromRequest;
        private readonly HashSet<string> _redactedKeysFromResponse;

        private List<FlexLogContext> _buffer;

        public FlexLogBackgroundService(FlexLogChannel logChannel, IOptions<FlexLogOptions> loggingOptions)
        {
            _logChannel = logChannel;

            _bufferLimit = loggingOptions.Value.BufferLimit ?? 1000;
            _timer = (loggingOptions.Value.TimerInSeconds ?? 5) * 1000;

            _redactedKeysFromRequest = loggingOptions.Value.LogDetails?.RequestBody?.RedactedKeys != null ?
                new HashSet<string>(loggingOptions.Value.LogDetails?.RequestBody?.RedactedKeys!, StringComparer.OrdinalIgnoreCase) :
                new HashSet<string>();

            _redactedKeysFromResponse = loggingOptions.Value.LogDetails?.ResponseBody?.RedactedKeys != null ?
                new HashSet<string>(loggingOptions.Value.LogDetails?.ResponseBody?.RedactedKeys!, StringComparer.OrdinalIgnoreCase) :
                new HashSet<string>();

            _buffer = new List<FlexLogContext>(_bufferLimit);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

                var readTask = _logChannel.Logs.Reader.WaitToReadAsync(cts.Token).AsTask();
                var timerTask = Task.Delay(_timer);

                var finishedTask = await Task.WhenAny(readTask, timerTask);

#if NET8_0_OR_GREATER
                await cts.CancelAsync();
#else
                cts.Cancel();
#endif
                cts.Dispose();

                if (finishedTask == readTask)
                {
                    while (_logChannel.Logs.Reader.TryRead(out var log))
                    {
                        _buffer.Add(log);
                        if (_buffer.Count >= _bufferLimit)
                        {
                            await FlushSinksAsync();
                            break;
                        }
                    }
                }
                else
                {
                    if (_buffer.Count > 0)
                    {
                        await FlushSinksAsync();
                    }
                }
            }

            while (_logChannel.Logs.Reader.TryRead(out var log))
            {
                _buffer.Add(log);
            }

            if (_buffer.Count > 0)
            {
                await FlushSinksAsync();
            }
        }

        private async Task FlushSinksAsync()
        {
            foreach (var logContext in _buffer)
            {
                if (logContext.RequestBodyRaw != null && !logContext.IsRequestBodyTooLarge!.Value)
                {
                    logContext.RequestBody = await ParseJson(logContext.RequestBodyRaw, _redactedKeysFromRequest);
                }

                if (logContext.ResponseBodyRaw != null)
                {
                    logContext.ResponseBody = await ParseJson(logContext.ResponseBodyRaw, _redactedKeysFromResponse);
                }
            }

            var sinks = _logChannel.Sinks.Select(s => s.FlushAsync(_buffer));
            await Task.WhenAll(sinks);
            _buffer.Clear();
        }

        private async Task<string?> ParseJson(byte[] rawData, HashSet<string> redactedKeys)
        {
            try
            {
                using var memoryStream = new MemoryStream(rawData);
                using var json = await JsonDocument.ParseAsync(memoryStream);

                if (redactedKeys.Count > 0)
                {
                    var redactedJson = RedactJsonKeys(json.RootElement, redactedKeys);
                    return JsonSerializer.Serialize(redactedJson);
                }

                return JsonSerializer.Serialize(json.RootElement);
            }
            catch
            {
                return _invalidJsonText;
            }
        }

        private object? RedactJsonKeys(JsonElement element, HashSet<string> redactedKeys)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var dictionary = new Dictionary<string, object?>();
                    foreach (var item in element.EnumerateObject())
                    {
                        if (redactedKeys.Contains(item.Name))
                        {
                            dictionary.Add(item.Name, _redactedText);
                        }
                        else
                        {
                            var value = RedactJsonKeys(item.Value, redactedKeys);
                            dictionary.Add(item.Name, value);
                        }
                    }

                    return dictionary;
                case JsonValueKind.Array:
                    var array = new List<object?>();
                    foreach (var item in element.EnumerateArray())
                    {
                        var value = RedactJsonKeys(item, redactedKeys);
                        array.Add(value);
                    }

                    return array;
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetDecimal(out var x))
                        return x;

                    return element.GetRawText();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Undefined:
                case JsonValueKind.Null:
                default:
                    return null;
            }
        }
    }
}
