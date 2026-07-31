namespace MainClient.Scheduler
{
    /// <summary>
    /// Produces an evenly phased deterministic click sequence. The half-click
    /// phase keeps the cumulative target within half a click instead of postponing
    /// every click to the end of its interval. The planner serializes its own state
    /// so all concurrent UV producers can safely share one task/hour instance.
    /// </summary>
    internal sealed class ClickRatioPlanner
    {
        private long _plannedDsp;
        private long _plannedClicks;
        private readonly object _sync = new();

        public ClickRatioPlanner(long existingDsp, long existingClicks)
        {
            _plannedDsp = Math.Max(0, existingDsp);
            _plannedClicks = Math.Max(0, existingClicks);
        }

        public bool PlanNext(double percentage)
        {
            if (!double.IsFinite(percentage))
            {
                return false;
            }

            lock (_sync)
            {
                var rate = Math.Clamp(percentage, 0d, 100d);
                _plannedDsp++;

                // decimal avoids the boundary errors produced by binary floating point.
                // Adding half a click centers clicks in the global task/hour stream.
                var targetClicks = (long)decimal.Floor(
                    _plannedDsp * (decimal)rate / 100m + 0.5m);

                if (_plannedClicks >= targetClicks)
                {
                    return false;
                }

                _plannedClicks++;
                return true;
            }
        }
    }
}
