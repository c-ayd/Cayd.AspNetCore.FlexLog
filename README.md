## About
FlexLog is an easy, flexible and detailed logging library that provides the logging infrastructure out of the box for ASP.NET Core. After setting up the library, you can create your own custom sink classes to store the buffered logs anywhere in any format you want.

## Quick Start
After installing the package, you need to create your own sink classes as well as fallback sinks if needed. To do that, you need to create a class inheriting from the `FlexLogSink` class and override the method called `WriteBatchAsync`. This method provides the buffered logs ready to be stored. In this method, you can use elements in the buffer to create your own format and save anywhere you want. You can also use `InitalizeAsync` and `DisposeAsync` methods the initialize and release the resources that your sink class uses if needed.

```csharp
public class MySink : FlexLogSink
{
    public override async Task WriteBatchAsync(IReadOnlyList<FlexLogContext> buffer)
    {
        // ... use 'buffer' to create your own format and save the logs.
    }

    public override async Task InitializeAsync()
    {
        // ... initialize resources if needed.
    }

    public override async Task DisposeAsync()
    {
        // ... release resources if needed.
    }
}
```

After creating your own sink and fallback sink classes, you need to register them in the dependency injection system and add the FlexLog's middleware to log HTTP requests automatically. It is recommended to add the FlexLog's middleware right after your global exception middleware to log thrown exceptions automatically as well since the FlexLog's middleware rethrows exceptions after catching them and logging them.

```csharp
/* Top-level statement */

using Cayd.AspNetCore.FlexLog.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
// Register FlexLog in the dependency system
builder.AddFlexLog(config =>
{
    // Add your own custom sinks
    config.AddSink(new MySink())
        .AddFallbackSink(new MyFallbackSink());
});

var app = builder.Build();
app.UseMiddleware<MyGlobalExceptionMiddleware>();
// Add FlexLog's middleware
app.UseFlexLog();


/* Program class and Main method */

using Cayd.AspNetCore.FlexLog.DependencyInjection;

public Startup(IConfiguration configuration)
{
    Configuration = configuration;
}

public IConfiguration Configuration { get; }

public void ConfigureServices(IServiceCollection services)
{
    // Register FlexLog in the dependency system
    services.AddFlexLog(Configuration, config =>
    {
        // Add your own custom sinks
        config.AddSink(new MySink())
            .AddFallbackSink(new MyFallbackSink());
    });
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseMiddleware<MyGlobalExceptionMiddleware>();
    // Add FlexLog's middleware
    app.UseFlexLog();
}
```

Afterwards, logs are automatically created by FlexLog, and it flushes buffered logs to the sinks you register. In addition to the automatically captured logging data, you can also inject `IFlexLogger<T>` to your services to add log entries to the current log scope, and they are saved in the `LogEntries` property of `FlexLogContext`.

```csharp
public class MyService : IMyService
{
    private readonly IFlexLogger<MyService> _flexLogger;

    public MyService(IFlexLogger<MyService> flexLogger)
    {
        _flexLogger = flexLogger;
    }

    public void MyMethod(int myParam)
    {
        if (myParam < 0)
        {
            // This log entry will be provided to the sink in the 'LogEntries' property of the related scope's FlexLogContext.
            _flexLogger.LogInformation("myParam is negative");
        }

        // ... code
    }
}
```

## Options & Optimizations
In order to customize FlexLog such as logging, ignoring or redacting specific data or routes etc., you need to define a configuration key called `FlexLog` in appsettings, user secrets or environment variables. When no configuration is defined, FlexLog uses the default configuration and logs everything. You can check out the [GitHub Wiki](https://github.com/c-ayd/Cayd.AspNetCore.FlexLog/wiki) to learn more about the options and the recommended optimizations.

## How It Works
- FlexLog does not interact with the ASP.NET Core's `ILoggingBuilder` and you can still have the other logging providers in your application. Instead, FlexLog creates its own backgroung services to handle logs in a non-blocking manner.
- The FlexLog's flushing mechanism depends on both a buffer size and a timer. The buffer size controls the threshold of how many logs trigger the flush process, while the timer controls how much time should pass since the last log to flush the buffer.

## Extras
FlexLog automatically logs only HTTP requests, however, it can be used to log other types of protocols. To do that, you need to create your own `FlexLogContext` during the communication and fill the values manually. Once it is completed, you need to add the log context to the FlexLog's log channel. So that, FlexLog handles it automatically at the background. To do that, you need to use the `AddLogContextToChannel` method in `FlexLogChannel`, which is a singleton service.