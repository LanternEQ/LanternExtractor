using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace LanternExtractor.Performance
{
    /// <summary>
    /// A thread-safe benchmarking utility to measure and report execution times of functions.
    /// </summary>
    public static class Benchmarker
    {
        // Counter for unique timing session IDs
        private static int _idCounter = 0;

        // Stores active timing sessions: ID -> (FunctionName, Stopwatch)
        private static ConcurrentDictionary<int, (string FunctionName, Stopwatch Stopwatch)> _timings
            = new ConcurrentDictionary<int, (string, Stopwatch)>();

        // Aggregated results: FunctionName -> (TotalTicks, Count)
        private static ConcurrentDictionary<string, (long TotalTicks, int Count)> _results
            = new ConcurrentDictionary<string, (long, int)>();

        /// <summary>
        /// Starts timing for a specified function.
        /// </summary>
        /// <param name="functionName">The name of the function to benchmark.</param>
        /// <returns>A unique identifier for the timing session.</returns>
        public static int Start(string functionName)
        {
            // Generate a unique ID atomically
            int id = System.Threading.Interlocked.Increment(ref _idCounter);

            // Initialize and start the stopwatch
            var stopwatch = Stopwatch.StartNew();

            // Record the timing session
            _timings[id] = (functionName, stopwatch);

            return id;
        }

        /// <summary>
        /// Stops timing for the specified session ID.
        /// </summary>
        /// <param name="id">The unique identifier returned by the Start method.</param>
        public static void Finished(int id)
        {
            // Attempt to remove the timing session
            if (_timings.TryRemove(id, out var timing))
            {
                // Stop the stopwatch
                timing.Stopwatch.Stop();

                // Get elapsed ticks
                long elapsedTicks = timing.Stopwatch.ElapsedTicks;

                // Aggregate results
                _results.AddOrUpdate(
                    timing.FunctionName,
                    (elapsedTicks, 1), // If function not present, add with current ticks and count 1
                    (key, existing) => (existing.TotalTicks + elapsedTicks, existing.Count + 1) // Otherwise, update
                );
            }
            else
            {
                // Handle invalid ID if necessary
                throw new ArgumentException($"Invalid Benchmarker ID: {id}");
            }
        }

        /// <summary>
        /// Generates a report of average execution times for each benchmarked function.
        /// </summary>
        /// <returns>A formatted string containing the performance report.</returns>
        public static string ReportAverageTimes()
        {
            var sb = new StringBuilder();
            sb.AppendLine("========== Function Performance Report ==========");
            sb.AppendLine($"Report Generated: {DateTime.Now}");
            sb.AppendLine("---------------------------------------------------");

            foreach (var entry in _results.OrderBy(e => e.Key))
            {
                string functionName = entry.Key;
                long totalTicks = entry.Value.TotalTicks;
                int count = entry.Value.Count;
                double averageTicks = (double)totalTicks / count;
                double averageMilliseconds = (averageTicks * 1000) / Stopwatch.Frequency;

                sb.AppendLine($"{functionName}: Average Time = {averageMilliseconds:F4} ms over {count} runs");
            }

            sb.AppendLine("===================================================");

            return sb.ToString();
        }

        /// <summary>
        /// Resets all benchmarking data, clearing active timings and results.
        /// </summary>
        public static void Reset()
        {
            _timings.Clear();
            _results.Clear();
            _idCounter = 0;
        }
    }
}
