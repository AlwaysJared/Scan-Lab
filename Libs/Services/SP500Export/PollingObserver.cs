using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Libs.Services.SP500Export
{
    public class PollingObserver : IDisposable
    {
        private readonly TimeSpan _pollInterval;
        private readonly Func<Task<bool>> _pollAction;
        private readonly ILogger? _logger;
        private CancellationTokenSource? _cts;
        private Task? _pollingTask;
        public bool IsComplete { get; private set; }

        public PollingObserver(TimeSpan pollInterval, Func<Task<bool>> pollAction, ILogger? logger = null)
        {
            _pollInterval = pollInterval;
            _pollAction = pollAction ?? throw new ArgumentNullException(nameof(pollAction));
            _logger = logger;
        }

        public void Start()
        {
            if (_pollingTask != null && !_pollingTask.IsCompleted)
                throw new InvalidOperationException("Polling already started.");

            _cts = new CancellationTokenSource();
            _pollingTask = Task.Run(() => PollLoopAsync(_cts.Token));
        }

        public void Stop()
        {
            IsComplete = true;
            _cts?.Cancel();
        }

        private async Task PollLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    bool result = await _pollAction();
                    if (result)
                    {
                        _logger?.LogDebug("Polling action returned true, stopping observer.");
                        Stop();
                        break;
                    }
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Polling action failed");
                }

                try
                {
                    await Task.Delay(_pollInterval, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
        }
    }
}
