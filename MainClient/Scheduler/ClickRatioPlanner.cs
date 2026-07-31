namespace MainClient.Scheduler
{
    /// <summary>
    /// Produces a deterministic click sequence whose cumulative rounding error is
    /// always less than one click. One instance must be used by only one caller at
    /// a time; TaskMetricsService supplies that synchronization.
    /// </summary>
    internal sealed class ClickRatioPlanner
    {
        private long _plannedDsp;
        private long _plannedClicks;

        public ClickRatioPlanner(long existingDsp, long existingClicks)
        {
            _plannedDsp = Math.Max(0, existingDsp);
            _plannedClicks = Math.Max(0, existingClicks);
        }

        public bool PlanNext(double percentage)
        {
            var rate = Math.Clamp(percentage, 0d, 100d);
            _plannedDsp++;

            // decimal avoids the boundary errors produced by binary floating point
            // (for example, a mathematically integral target becoming 2.999999...).
            var targetClicks = (long)decimal.Floor(
                _plannedDsp * (decimal)rate / 100m);

            if (_plannedClicks >= targetClicks)
            {
                return false;
            }

            _plannedClicks++;
            return true;
        }
    }
}
