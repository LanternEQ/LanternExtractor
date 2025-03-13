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

        // Store the current console window width for detecting changes
        private int _currentWindowWidth;

        private string Title => $"{StatusText} {StatusPercent} | {AppDomain.CurrentDomain.FriendlyName}";
        private double Percentage => (double)_currentStep / _totalSteps * 100;
        private string StatusPercent => $"{_currentStep}/{_totalSteps} ({Percentage:0.00}%)";
        private string StatusText => !_isMultithreaded && !string.IsNullOrEmpty(_currentFileName) ? $"Extracting: {_currentFileName}" : "Extracting archives...";

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

            // Capture the initial window width
            _currentWindowWidth = Console.WindowWidth;

            // Start with a clean buffer (addresses existing scrollback)
            Console.Clear();

            // Initial progress bar draw
            Draw(initialDraw: true);

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
            string timerString = $"{(int)elapsedTime.TotalMinutes}:{elapsedTime.Seconds:D2}";

            int cursorLeft = Console.CursorLeft;
            int cursorTop = Console.CursorTop;

            // Check if the console window size has changed
            int newWindowWidth = Console.WindowWidth;
            if (newWindowWidth != _currentWindowWidth)
            {
                Redraw();
                _currentWindowWidth = newWindowWidth;
            }

            // Calculate the new position for the timer
            int timerPosition = _currentWindowWidth - timerString.Length - 1;
            if (timerPosition > 0)
            {
                Console.SetCursorPosition(timerPosition, cursorTop);
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
                    Draw(initialDraw: false);
                    return;
                }

                if (advanceBar) _currentStep++;

                Draw(initialDraw: true);

                if (_currentStep == _totalSteps)
                {
                    Complete();
                }
            }
        }

        private void Draw(bool initialDraw)
        {
            int filledWidth = (int)((double)_currentStep / _totalSteps * _barWidth);

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

                    Console.Write($"] {StatusPercent}");
                }

                // Status update
                string status = _isCompleted ? "Extraction complete" : StatusText;
                if (_lastPrintedStatus != status)
                {
                    Console.SetCursorPosition(0, originalCursorTop + 1);
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.SetCursorPosition(0, originalCursorTop + 1);
                    Console.Write(status);
                    _lastPrintedStatus = status;
                }

                Console.Title = Title;
                Console.SetCursorPosition(originalCursorLeft, originalCursorTop);
            }
        }

        private void Redraw()
        {
            Console.Clear();
            _lastPrintedStatus = string.Empty;
            Draw(initialDraw: true);
        }

        private void Complete()
        {
            lock (_lock)
            {
                _isCompleted = true;
                _currentStep = _totalSteps;
                _currentFileName = "Extraction complete";
                Draw(initialDraw: true);

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

                Draw(initialDraw: false);
            }
        }
    }
}
