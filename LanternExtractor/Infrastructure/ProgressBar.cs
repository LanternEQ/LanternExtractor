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
        private readonly ConsoleColor _resetColorForeground;
        private readonly ConsoleColor _resetColorBackground;
        private readonly DateTime _startTime;

        private readonly object _lock = new object();
        private bool _isCompleted;
        private Thread _timerThread;
        private string _currentFileName;
        private string _lastPrintedStatus = string.Empty;
        private readonly bool _isMultithreaded;
        private bool _firstStepCalled = false;

        public ProgressBar(int totalSteps, int barWidth, bool isMultithreaded = false, char fillChar = '#', char backgroundChar = '-', ConsoleColor fillColor = ConsoleColor.Green, ConsoleColor backgroundColor = ConsoleColor.DarkGray)
        {
            _totalSteps = totalSteps;
            _barWidth = barWidth;
            _fillChar = fillChar;
            _backgroundChar = backgroundChar;
            _fillColor = fillColor;
            _backgroundColor = backgroundColor;
            _currentStep = 0;
            _resetColorForeground = Console.ForegroundColor;
            _resetColorBackground = Console.BackgroundColor;
            _startTime = DateTime.Now;
            _isCompleted = false;
            _currentFileName = string.Empty;
            _isMultithreaded = isMultithreaded;

            // Draw the initial empty progress bar immediately
            Draw(_currentFileName, true);

            // Start the timer thread to update the time independently
            _timerThread = new Thread(UpdateTimer)
            {
                IsBackground = true
            };
            _timerThread.Start();
        }

        // Timer to update the timer part independently
        private void UpdateTimer()
        {
            while (!_isCompleted)
            {
                lock (_lock)
                {
                    UpdateTimerDisplay();
                }
                Thread.Sleep(1000); // Update the timer once every second
            }
        }

        // Updates just the timer part of the display
        private void UpdateTimerDisplay()
        {
            TimeSpan elapsedTime = DateTime.Now - _startTime;
            string timerString = $"{elapsedTime.Minutes}:{elapsedTime.Seconds:D2}";

            // Position the cursor for the timer update
            int cursorLeft = Console.CursorLeft;
            int cursorTop = Console.CursorTop;

            // Set the cursor to the correct position and update the timer
            int remainingSpace = Console.WindowWidth - timerString.Length - 1;
            if (remainingSpace > 0)
            {
                Console.SetCursorPosition(Console.WindowWidth - timerString.Length - 1, cursorTop);
                Console.Write(timerString);
            }

            // Reset the cursor to its original position
            Console.SetCursorPosition(cursorLeft, cursorTop);
        }

        // Step method that takes a parameter to optionally advance the progress bar
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

                // First step for single-threaded mode updates the text, not the bar
                if (!_isMultithreaded && !_firstStepCalled)
                {
                    _firstStepCalled = true;
                    Draw(fileName, false);
                    return;
                }

                if (advanceBar)
                {
                    _currentStep++;
                }

                // Update the progress bar
                Draw(fileName, advanceBar);

                if (_currentStep == _totalSteps)
                {
                    Complete();
                }
            }
        }

        // Draws the progress bar and status
        private void Draw(string fileName, bool drawBar)
        {
            int filledWidth = (int)((double)_currentStep / _totalSteps * _barWidth);
            double percentage = (double)_currentStep / _totalSteps * 100;

            lock (_lock)
            {
                Console.CursorVisible = false;

                int originalCursorLeft = Console.CursorLeft;
                int originalCursorTop = Console.CursorTop;

                Console.SetCursorPosition(0, originalCursorTop);

                // Draw progress bar if requested
                if (drawBar)
                {
                    Console.Write('[');
                    Console.ForegroundColor = _fillColor;
                    for (int i = 0; i < filledWidth; i++)
                    {
                        Console.Write(_fillChar);
                    }

                    Console.ForegroundColor = _backgroundColor;
                    for (int i = filledWidth; i < _barWidth; i++)
                    {
                        Console.Write(_backgroundChar);
                    }
                    Console.ResetColor();

                    Console.Write($"] {_currentStep}/{_totalSteps} ({percentage:0.00}%)");
                }

                // Show extracting file in single-threaded mode
                if (!_isMultithreaded && !string.IsNullOrEmpty(fileName))
                {
                    Console.SetCursorPosition(0, originalCursorTop + 1);
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.SetCursorPosition(0, originalCursorTop + 1);
                    Console.Write($"Extracting: {fileName}");
                }

                // Multithreaded mode shows "Extracting archives..." or "Extraction complete" when it changes
                string status = _isCompleted ? "Extraction complete" : "Extracting archives...";
                if (_lastPrintedStatus != status)
                {
                    Console.SetCursorPosition(0, originalCursorTop + 1);
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.SetCursorPosition(0, originalCursorTop + 1);
                    Console.Write(status);
                    _lastPrintedStatus = status; // Store last printed status to avoid flickering
                }

                // Reset cursor to original position to avoid screen shifting
                Console.SetCursorPosition(originalCursorLeft, originalCursorTop);
            }
        }

        // Called when all steps are complete
        public void Complete()
        {
            lock (_lock)
            {
                _isCompleted = true;
                _currentStep = _totalSteps;
                Draw("Extraction complete", true);

                _timerThread.Join();

                Console.SetCursorPosition(0, Console.CursorTop + 2);
                Console.CursorVisible = true;
                Console.ForegroundColor = _resetColorForeground;
                Console.BackgroundColor = _resetColorBackground;
            }
        }

        // Resets the progress bar to its initial state
        public void Reset()
        {
            lock (_lock)
            {
                _currentStep = 0;
                _currentFileName = "";
                _isCompleted = false;
                _firstStepCalled = false;
                _lastPrintedStatus = "";
                Draw("", false);
            }
        }
    }
}
