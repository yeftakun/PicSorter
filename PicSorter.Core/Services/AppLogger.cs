using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace PicSorter.Core.Services
{
    /// <summary>
    /// A simple file-based logger provider that writes to %LocalAppData%\PicSorter\logs\.
    /// Usage: AppLogger.Factory.CreateLogger&lt;MyClass&gt;()
    /// </summary>
    public static class AppLogger
    {
        private static readonly string LogDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PicSorter", "logs");

        public static ILoggerFactory Factory { get; } = CreateFactory();

        private static ILoggerFactory CreateFactory()
        {
            Directory.CreateDirectory(LogDir);

            return LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddConsole();

                // Simple rolling-daily file provider via a custom writer
                builder.AddProvider(new DailyFileLoggerProvider(LogDir));
            });
        }
    }

    // ─── Simple daily-file provider ─────────────────────────────────────────

    internal sealed class DailyFileLoggerProvider : ILoggerProvider
    {
        private readonly string _logDir;

        public DailyFileLoggerProvider(string logDir)
        {
            _logDir = logDir;
        }

        public ILogger CreateLogger(string categoryName) =>
            new DailyFileLogger(_logDir, categoryName);

        public void Dispose() { }
    }

    internal sealed class DailyFileLogger : ILogger
    {
        private readonly string _logDir;
        private readonly string _category;
        private static readonly object _lock = new();

        public DailyFileLogger(string logDir, string category)
        {
            _logDir = logDir;
            _category = category;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            string fileName = $"picsorter-{DateTime.Now:yyyy-MM-dd}.log";
            string filePath = Path.Combine(_logDir, fileName);
            string message = formatter(state, exception);
            string line = $"[{DateTime.Now:HH:mm:ss.fff}] [{logLevel,-11}] [{_category}] {message}";
            if (exception != null)
                line += $"\n  {exception}";

            lock (_lock)
            {
                try { File.AppendAllText(filePath, line + "\n"); }
                catch { /* never crash the app because logging fails */ }
            }
        }
    }
}
