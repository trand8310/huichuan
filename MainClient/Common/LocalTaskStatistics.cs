using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace MainClient.Common;

/// <summary>
/// A thread-safe, extensible set of named local counters with durable totals.
/// Session values reset for each run; total values survive application restarts.
/// </summary>
public sealed class LocalTaskStatistics
{
    private readonly ConcurrentDictionary<string, Counter> session = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Counter> totals = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim persistenceGate = new(1, 1);
    private readonly string filePath;

    public LocalTaskStatistics(string directory, string scope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);

        var invalidCharacters = Path.GetInvalidFileNameChars();
        var safeScope = new string(scope.Select(character =>
            invalidCharacters.Contains(character) ? '_' : character).ToArray());
        filePath = Path.Combine(directory, $"statistics_{safeScope}.json");
    }

    public long Increment(string name, long value = 1)
    {
        Validate(name, value);
        var sessionValue = Interlocked.Add(ref GetCounter(session, name).Value, value);
        Interlocked.Add(ref GetCounter(totals, name).Value, value);
        return sessionValue;
    }

    public long GetSessionValue(string name) => Read(session, name);

    public long GetTotalValue(string name) => Read(totals, name);

    public LocalTaskStatisticsSnapshot GetSnapshot()
    {
        return new LocalTaskStatisticsSnapshot(
            CopyValues(session),
            CopyValues(totals),
            DateTimeOffset.UtcNow);
    }

    public void ResetSession() => session.Clear();

    public bool TryLoad(out Exception? error)
    {
        error = null;
        if (!File.Exists(filePath))
            return true;

        try
        {
            var root = JObject.Parse(File.ReadAllText(filePath));
            var savedTotals = root["Totals"] as JObject;
            if (savedTotals is not null)
            {
                foreach (var property in savedTotals.Properties())
                {
                    var value = property.Value.Value<long>();
                    if (value >= 0)
                        Interlocked.Exchange(ref GetCounter(totals, property.Name).Value, value);
                }
            }
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
            JsonException or FormatException or OverflowException)
        {
            error = exception;
            return false;
        }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await persistenceGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        var temporaryPath = filePath + ".tmp";
        try
        {
            var snapshot = GetSnapshot();
            var document = new
            {
                Version = 1,
                Totals = snapshot.Totals,
                LastSavedUtc = snapshot.CapturedAtUtc
            };
            var json = JsonConvert.SerializeObject(document, Formatting.Indented);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllTextAsync(temporaryPath, json, cancellationToken).ConfigureAwait(false);
            File.Move(temporaryPath, filePath, overwrite: true);
        }
        finally
        {
            TryDelete(temporaryPath);
            persistenceGate.Release();
        }
    }

    private static Counter GetCounter(
        ConcurrentDictionary<string, Counter> counters,
        string name) => counters.GetOrAdd(name, static _ => new Counter());

    private static long Read(ConcurrentDictionary<string, Counter> counters, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return counters.TryGetValue(name, out var counter)
            ? Interlocked.Read(ref counter.Value)
            : 0;
    }

    private static IReadOnlyDictionary<string, long> CopyValues(
        ConcurrentDictionary<string, Counter> counters) =>
        counters.ToDictionary(
            pair => pair.Key,
            pair => Interlocked.Read(ref pair.Value.Value),
            StringComparer.Ordinal);

    private static void Validate(string name, long value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(value);
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed class Counter
    {
        public long Value;
    }
}

public sealed record LocalTaskStatisticsSnapshot(
    IReadOnlyDictionary<string, long> Session,
    IReadOnlyDictionary<string, long> Totals,
    DateTimeOffset CapturedAtUtc)
{
    public long GetSessionValue(string name) => Session.GetValueOrDefault(name);
    public long GetTotalValue(string name) => Totals.GetValueOrDefault(name);
}

public static class TaskStatisticNames
{
    public const string TasksFetched = "tasks-fetched";
    public const string Requests = "ad-requests";
    public const string Processed = "tasks-processed";
    public const string Exposures = "exposures";
    public const string Clicks = "clicks";
}
