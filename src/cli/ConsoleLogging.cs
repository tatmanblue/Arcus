using Microsoft.Extensions.Logging;

namespace ArcusCli;

/// <summary>
/// Providers cleaner output to console as we do not need structured logging for CLI app
/// </summary>
public class ConsoleLogger(string name) : ILogger
{
    public IDisposable BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        string message = formatter(state, exception);
        Console.WriteLine($"{message}");
    }
}

public class ConsoleLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new ConsoleLogger(categoryName);
    }

    public void Dispose() { }
}