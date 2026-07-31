namespace MainClient.Common;

/// <summary>
/// Thread-safe counters and click-rate decisions for one advertising task.
/// </summary>
public sealed class TaskExposure
{
    private readonly object submissionGate = new();
    private long requestCount;
    private long submissionCount;
    private long exposureCount;
    private long clickCount;
    private long scheduledClickCount;

    public long RequestCount => Interlocked.Read(ref requestCount);
    public long SubmissionCount => Interlocked.Read(ref submissionCount);
    public long ExposureCount => Interlocked.Read(ref exposureCount);
    public long ClickCount => Interlocked.Read(ref clickCount);
    public long ScheduledClickCount => Interlocked.Read(ref scheduledClickCount);

    public long AddRequests(int count = 1) => AddPositive(ref requestCount, count);

    public long AddExposures(int count = 1) => AddPositive(ref exposureCount, count);

    public long AddClicks(int count = 1) => AddPositive(ref clickCount, count);

    /// <summary>
    /// Atomically reserves a submission and decides whether it should click. The
    /// reservation must be committed after IPC succeeds; otherwise disposing it
    /// rolls the counters back.
    /// </summary>
    public SubmissionReservation ReserveSubmission(int targetClickRate, bool mayClick)
    {
        targetClickRate = Math.Clamp(targetClickRate, 0, 100);

        lock (submissionGate)
        {
            var nextSubmissionCount = checked(submissionCount + 1);
            var click = mayClick && ShouldScheduleClick(
                targetClickRate,
                nextSubmissionCount,
                scheduledClickCount);

            submissionCount = nextSubmissionCount;
            if (click)
                scheduledClickCount = checked(scheduledClickCount + 1);

            return new SubmissionReservation(
                this,
                click,
                CalculateRate(scheduledClickCount, submissionCount));
        }
    }

    public TaskExposureSnapshot GetSnapshot()
    {
        lock (submissionGate)
        {
            return new TaskExposureSnapshot(
                RequestCount,
                submissionCount,
                ExposureCount,
                ClickCount,
                scheduledClickCount);
        }
    }

    private static bool ShouldScheduleClick(
        int targetClickRate,
        long nextSubmissionCount,
        long currentScheduledClicks)
    {
        if (targetClickRate <= 0)
            return false;

        if (targetClickRate == 100 || currentScheduledClicks == 0 || nextSubmissionCount == 1)
            return true;

        return CalculateRate(currentScheduledClicks + 1, nextSubmissionCount) < targetClickRate;
    }

    private static double CalculateRate(long clicks, long submissions) =>
        submissions == 0 ? 0 : clicks * 100d / submissions;

    private static long AddPositive(ref long counter, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return Interlocked.Add(ref counter, count);
    }

    private void RollbackSubmission(bool click)
    {
        lock (submissionGate)
        {
            if (submissionCount <= 0 || (click && scheduledClickCount <= 0))
                throw new InvalidOperationException("曝光提交预留状态不一致，无法回滚。");

            submissionCount--;
            if (click)
                scheduledClickCount--;
        }
    }

    public sealed class SubmissionReservation : IDisposable
    {
        private TaskExposure? owner;

        internal SubmissionReservation(TaskExposure owner, bool click, double clickThroughRate)
        {
            this.owner = owner;
            Click = click;
            ClickThroughRate = clickThroughRate;
        }

        public bool Click { get; }
        public double ClickThroughRate { get; }

        public void Commit() => Interlocked.Exchange(ref owner, null);

        public void Dispose()
        {
            var currentOwner = Interlocked.Exchange(ref owner, null);
            currentOwner?.RollbackSubmission(Click);
        }
    }
}

public readonly record struct TaskExposureSnapshot(
    long RequestCount,
    long SubmissionCount,
    long ExposureCount,
    long ClickCount,
    long ScheduledClickCount)
{
    public double ClickThroughRate =>
        SubmissionCount == 0 ? 0 : ScheduledClickCount * 100d / SubmissionCount;
}
