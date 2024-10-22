using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Filters;

namespace LanternExtractor.Infrastructure
{
    public static class LogHelper
    {
        private static LoggingLevelSwitch _loggingLevelSwitch = new LoggingLevelSwitch();

        public static void InitializeLogging(LogEventLevel initialLogLevel)
        {
            _loggingLevelSwitch.MinimumLevel = initialLogLevel; // Set the initial log level

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(_loggingLevelSwitch) // Use the level switch to control logging level
                .Enrich.FromLogContext()
                .WriteTo.File("log.txt",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                    restrictedToMinimumLevel: LogEventLevel
                        .Verbose) // Log everything, but filter using the level switch
                .Filter
                .ByExcluding(Matching.WithProperty<bool>("NoLog", p => p == true)) // Exclude logs with NoLog property
                .CreateLogger();
        }

        public static void SetLogLevel(int logLevel)
        {
            LogEventLevel verbosity;

            switch (logLevel)
            {
                case 0:
                    verbosity = LogEventLevel.Information;
                    break;
                case 1:
                    verbosity = LogEventLevel.Warning;
                    break;
                default:
                    verbosity = LogEventLevel.Error;
                    break;
            }

            _loggingLevelSwitch.MinimumLevel = verbosity;
        }
    }
}
