using System.Timers;

namespace QuranBlazor.Services
{
    /// <summary>
    /// Helper class for debouncing rapid function calls (e.g., search input)
    /// </summary>
    public class DebounceTimer : IDisposable
    {
        private System.Timers.Timer _timer;
        private readonly int _delayMilliseconds;

        public DebounceTimer(int delayMilliseconds = 300)
        {
            _delayMilliseconds = delayMilliseconds;
        }

        /// <summary>
        /// Debounces the action. If called multiple times rapidly, only executes once after the delay.
        /// </summary>
        /// <param name="action">The action to execute after the delay</param>
        public void Debounce(Action action)
        {
            _timer?.Stop();
            _timer?.Dispose();

            _timer = new System.Timers.Timer(_delayMilliseconds);
            _timer.Elapsed += (sender, args) =>
            {
                _timer?.Stop();
                _timer?.Dispose();
                action?.Invoke();
            };
            _timer.AutoReset = false;
            _timer.Start();
        }

        public void Dispose()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }
    }
}
