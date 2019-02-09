using System.IO;

namespace LanternExtractor.Infrastructure.Logger
{
    /// <summary>
    /// Logs the text to a file
    /// </summary>
    public class TextFileLogger : ILogger
    {
        private readonly StreamWriter _streamWriter;

        public LogVerbosity Verbosity { get; set; }

        public TextFileLogger(string logFilePath, LogVerbosity verbosity)
        {
            _streamWriter = new StreamWriter(logFilePath);
            Verbosity = verbosity;
            _streamWriter.AutoFlush = true;
        }


        public void LogInfo(string message)
        {
            if (Verbosity > LogVerbosity.Info)
            {
                return;
            }

            _streamWriter.WriteLine("<INFO> " + message);
        }

        public void LogWarning(string message)
        {
            if (Verbosity > LogVerbosity.Warning)
            {
                return;
            }

            _streamWriter.WriteLine("<WARN> " + message);
        }

        public void LogError(string message)
        {
            _streamWriter.WriteLine("<ERROR> " + message);
        }
    }
}