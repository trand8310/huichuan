using System.Threading.Channels;

namespace Huichuan.Protocol;

/// <summary>
/// Processes copied WM_COPYDATA payloads with a fixed number of workers. This keeps
/// the window procedure short and avoids creating an unbounded number of ThreadPool tasks.
/// </summary>
public sealed class CopyDataMessageQueue : IDisposable
{
    private const int DefaultCapacity = 16_384;
    private readonly Channel<string> channel;
    private readonly CancellationTokenSource cancellation = new();
    private readonly Task[] workers;

    public CopyDataMessageQueue(
        Action<string> messageHandler,
        Action<Exception>? errorHandler = null,
        int workerCount = 1)
    {
        ArgumentNullException.ThrowIfNull(messageHandler);
        ArgumentOutOfRangeException.ThrowIfLessThan(workerCount, 1);

        channel = Channel.CreateBounded<string>(new BoundedChannelOptions(DefaultCapacity)
        {
            SingleReader = workerCount == 1,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait,
            AllowSynchronousContinuations = false
        });
        workers = Enumerable.Range(0, workerCount)
            .Select(_ => Task.Run(() => ProcessMessagesAsync(messageHandler, errorHandler)))
            .ToArray();
    }

    public bool TryEnqueue(string message) =>
        !string.IsNullOrWhiteSpace(message) && channel.Writer.TryWrite(message);

    private async Task ProcessMessagesAsync(
        Action<string> messageHandler,
        Action<Exception>? errorHandler)
    {
        try
        {
            await foreach (var message in channel.Reader.ReadAllAsync(cancellation.Token))
            {
                try
                {
                    messageHandler(message);
                }
                catch (Exception exception)
                {
                    errorHandler?.Invoke(exception);
                }
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
    }

    public void Dispose()
    {
        channel.Writer.TryComplete();
        cancellation.Cancel();
        cancellation.Dispose();
    }
}
