using Cayd.AspNetCore.FlexLog.Logging;
using Cayd.AspNetCore.FlexLog.Options;
using Cayd.AspNetCore.FlexLog.Sinks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Cayd.AspNetCore.FlexLog.Services
{
    public class FlexLogBackgroundService : BackgroundService
    {
        private static readonly string _invalidJsonText = "INVALID JSON";
        private static readonly string _redactedText = "REDACTED";

        private static readonly int _defaultBufferLimit = 1000;

        private readonly FlexLogChannel _logChannel;
        private readonly ILogger<FlexLogBackgroundService> _logger;

        private readonly int _bufferLimit;
        private readonly int _timer;
        private readonly HashSet<string> _redactedKeysFromRequest;
        private readonly HashSet<string> _redactedKeysFromResponse;

        private List<FlexLogContext> _buffer;

        public FlexLogBackgroundService(FlexLogChannel logChannel,
            ILogger<FlexLogBackgroundService> logger,
            IOptions<FlexLogOptions> loggingOptions)
        {
            _logChannel = logChannel;
            _logger = logger;

            _bufferLimit = loggingOptions.Value.BufferLimit ?? _defaultBufferLimit;
            _timer = (loggingOptions.Value.TimerInSeconds ?? 5) * 1000;

            _redactedKeysFromRequest = loggingOptions.Value.LogDetails?.RequestBody?.RedactedKeys != null ?
                new HashSet<string>(loggingOptions.Value.LogDetails?.RequestBody?.RedactedKeys!, StringComparer.OrdinalIgnoreCase) :
                new HashSet<string>();

            _redactedKeysFromResponse = loggingOptions.Value.LogDetails?.ResponseBody?.RedactedKeys != null ?
                new HashSet<string>(loggingOptions.Value.LogDetails?.ResponseBody?.RedactedKeys!, StringComparer.OrdinalIgnoreCase) :
                new HashSet<string>();

            _buffer = new List<FlexLogContext>(_bufferLimit);
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var sink in _logChannel.Sinks)
            {
                await sink.InitializeAsync();
            }

            foreach (var fallbackSink in _logChannel.FallbackSinks)
            {
                await fallbackSink.InitializeAsync();
            }

            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var sink in _logChannel.Sinks)
            {
                await sink.DisposeAsync();
            }

            foreach (var fallbackSink in _logChannel.FallbackSinks)
            {
                await fallbackSink.DisposeAsync();
            }

            await base.StopAsync(cancellationToken);
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

            var isSuccessful = await RunSinkTasks(_logChannel.Sinks);
            if (!isSuccessful)
            {
                await RunSinkTasks(_logChannel.FallbackSinks);
            }

            _buffer.Clear();
        }

        private async Task<bool> RunSinkTasks(IReadOnlyList<FlexLogSink> sinks)
        {
            var tasks = sinks
                .Select(s => new
                {
                    s.GetType().Name,
                    Task = s.FlushAsync(_buffer)
                })
                .ToList();

            try
            {
                await Task.WhenAll(tasks.Select(t => t.Task));
                return true;
            }
            catch
            {
                var faultedTasks = tasks
                    .Where(t => t.Task.IsFaulted)
                    .ToList();

                foreach (var faultedTask in faultedTasks)
                {
                    _logger.LogError(faultedTask.Task.Exception?.InnerException, $"{faultedTask.Name} threw an exception: " + faultedTask.Task.Exception?.InnerException?.Message);
                }

                return faultedTasks.Count == tasks.Count;
            }
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
