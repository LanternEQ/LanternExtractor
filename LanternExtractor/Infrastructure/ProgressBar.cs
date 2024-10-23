using System;
using System.Threading;

namespace LanternExtractor.Infrastructure
{
    public class ProgressBar
    {
        private readonly int _totalSteps;
        private int _currentStep;
        private readonly int _barWidth;
        private readonly char _fillChar;
        private readonly char _backgroundChar;
        private readonly ConsoleColor _fillColor;
        private readonly ConsoleColor _backgroundColor;
        private readonly ConsoleColor _defaultForegroundColor;
        private readonly ConsoleColor _defaultBackgroundColor;
        private readonly DateTime _startTime;
        private readonly object _lock = new object();
        private bool _isCompleted;
        private Thread _timerThread;
        private string _currentFileName;
        private string _lastPrintedStatus = string.Empty;
        private readonly bool _isMultithreaded;
        private bool _firstStepCalled;

        public ProgressBar(int totalSteps, int barWidth, bool isMultithreaded = false, char fillChar = '#', char backgroundChar = '-', ConsoleColor fillColor = ConsoleColor.Green, ConsoleColor backgroundColor = ConsoleColor.DarkGray)
        {
            _totalSteps = totalSteps;
            _barWidth = barWidth;
            _fillChar = fillChar;
            _backgroundChar = backgroundChar;
            _fillColor = fillColor;
            _backgroundColor = backgroundColor;
            _defaultForegroundColor = Console.ForegroundColor;
            _defaultBackgroundColor = Console.BackgroundColor;
            _startTime = DateTime.Now;
            _isMultithreaded = isMultithreaded;
            _isCompleted = false;
            _firstStepCalled = false;

            // Initial progress bar draw
            Draw(_currentFileName, initialDraw: true);

            // Start timer in background
            _timerThread = new Thread(UpdateTimer)
            {
                IsBackground = true
            };
            _timerThread.Start();
        }

        private void UpdateTimer()
        {
            DateTime lastUpdateTime = DateTime.Now;

            while (!_isCompleted)
            {
                lock (_lock)
                {
                    UpdateTimerDisplay();
                }

                var now = DateTime.Now;
                var elapsedSinceLastUpdate = now - lastUpdateTime;

                // Sleep for remaining time in the second
                if (elapsedSinceLastUpdate.TotalMilliseconds < 1000)
                {
                    var remainingTime = 1000 - (int)elapsedSinceLastUpdate.TotalMilliseconds;
                    Thread.Sleep(remainingTime);
                }

                lastUpdateTime = DateTime.Now;
            }
        }

        private void UpdateTimerDisplay()
        {
            TimeSpan elapsedTime = DateTime.Now - _startTime;
            string timerString = $"{elapsedTime.Minutes}:{elapsedTime.Seconds:D2}";

            int cursorLeft = Console.CursorLeft;
            int cursorTop = Console.CursorTop;

            // Update the timer display at the right side of the console
            if (Console.WindowWidth - timerString.Length - 1 > 0)
            {
                Console.SetCursorPosition(Console.WindowWidth - timerString.Length - 1, cursorTop);
                Console.Write(timerString);
            }

            // Reset the cursor to its original position
            Console.SetCursorPosition(cursorLeft, cursorTop);
        }

        public void Step(string fileName, bool advanceBar = true)
        {
            lock (_lock)
            {
                _currentFileName = fileName;

                if (string.IsNullOrEmpty(fileName))
                {
                    Complete();
                    return;
                }

                if (!_isMultithreaded && !_firstStepCalled)
                {
                    _firstStepCalled = true;
                    Draw(fileName, initialDraw: false);
                    return;
                }

                if (advanceBar) _currentStep++;

                Draw(fileName, initialDraw: true);

                if (_currentStep == _totalSteps)
                {
                    Complete();
                }
            }
        }

        private void Draw(string fileName, bool initialDraw)
        {
            int filledWidth = (int)((double)_currentStep / _totalSteps * _barWidth);
            double percentage = (double)_currentStep / _totalSteps * 100;

            lock (_lock)
            {
                Console.CursorVisible = false;

                int originalCursorLeft = Console.CursorLeft;
                int originalCursorTop = Console.CursorTop;

                Console.SetCursorPosition(0, originalCursorTop);

                // Draw progress bar
                if (initialDraw)
                {
                    Console.Write('[');
                    Console.ForegroundColor = _fillColor;

                    Console.Write(new string(_fillChar, filledWidth));
                    Console.ForegroundColor = _backgroundColor;
                    Console.Write(new string(_backgroundChar, _barWidth - filledWidth));
                    Console.ResetColor();

                    Console.Write($"] {_currentStep}/{_totalSteps} ({percentage:0.00}%)");
                }

                // Display extracting file in single-threaded mode
                if (!_isMultithreaded && !string.IsNullOrEmpty(fileName))
                {
                    Console.SetCursorPosition(0, originalCursorTop + 1);
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.SetCursorPosition(0, originalCursorTop + 1);
                    Console.Write($"Extracting: {fileName}");
                }

                // Status update in multithreaded mode
                string status = _isCompleted ? "Extraction complete" : "Extracting archives...";
                if (_lastPrintedStatus != status)
                {
                    Console.SetCursorPosition(0, originalCursorTop + 1);
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.SetCursorPosition(0, originalCursorTop + 1);
                    Console.Write(status);
                    _lastPrintedStatus = status;
                }

                Console.SetCursorPosition(originalCursorLeft, originalCursorTop);
            }
        }

        private void Complete()
        {
            lock (_lock)
            {
                _isCompleted = true;
                _currentStep = _totalSteps;
                Draw("Extraction complete", initialDraw: true);

                _timerThread.Join();

                Console.SetCursorPosition(0, Console.CursorTop + 2);
                Console.CursorVisible = true;
                Console.ForegroundColor = _defaultForegroundColor;
                Console.BackgroundColor = _defaultBackgroundColor;
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _currentStep = 0;
                _currentFileName = string.Empty;
                _isCompleted = false;
                _firstStepCalled = false;
                _lastPrintedStatus = string.Empty;

                Draw(string.Empty, initialDraw: false);
            }
        }
    }
}
