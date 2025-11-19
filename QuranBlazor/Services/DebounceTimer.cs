using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;

namespace QuranBlazor.Services
{
    /// <summary>
    /// Helper class for debouncing rapid function calls (e.g., search input)
    /// </summary>
    public class DebounceTimer : IDisposable
    {
        private readonly int _delayMilliseconds;
        private CancellationTokenSource _cts = new();
        private bool _disposed;

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
            if (action == null) return;
            Debounce(async () =>
            {
                action();
                await Task.CompletedTask;
            });
        }

        /// <summary>
        /// Debounces an async action. If called multiple times rapidly, only executes once after the delay.
        /// </summary>
        /// <param name="action">The async action to execute after the delay</param>
        public void Debounce(Func<Task> action)
        {
            if (action == null || _disposed) return;

            var previousToken = Interlocked.Exchange(ref _cts, new CancellationTokenSource());
            previousToken.Cancel();
            previousToken.Dispose();

            var localCts = _cts;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(_delayMilliseconds, localCts.Token).ConfigureAwait(false);
                    if (localCts.IsCancellationRequested) return;

                    await MainThread.InvokeOnMainThreadAsync(action).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Swallow cancellation - expected during rapid calls
                }
            });
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            var current = Interlocked.Exchange(ref _cts, new CancellationTokenSource());
            current.Cancel();
            current.Dispose();
            _disposed = true;
        }
    }
}
