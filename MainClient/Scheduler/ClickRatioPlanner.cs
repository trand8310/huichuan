namespace MainClient.Scheduler
{
    /// <summary>
    /// Produces an evenly phased deterministic click sequence. The half-click
    /// phase keeps the cumulative target within half a click instead of postponing
    /// every click to the end of its interval. TaskMetricsService serializes calls
    /// for the shared task/hour instance.
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

        public bool PlanNext(double percentage, long confirmedDsp, long confirmedClicks)
        {
            if (!double.IsFinite(percentage))
            {
                return false;
            }

            var rate = Math.Clamp(percentage, 0d, 100d);
            confirmedDsp = Math.Max(0, confirmedDsp);
            confirmedClicks = Math.Max(0, confirmedClicks);

            var confirmedTarget = (long)decimal.Floor(
                confirmedDsp * (decimal)rate / 100m + 0.5m);

            // Browser confirmations can arrive after a decision, and a restarted
            // client can restore a CTR already above target. In that case planned
            // reservations must not issue another click: follow confirmed traffic
            // until impressions have brought the real CTR back to the target.
            if (confirmedClicks > confirmedTarget)
            {
                _plannedDsp = confirmedDsp + 1;
                _plannedClicks = confirmedClicks;
                return false;
            }

            // Incorporate confirmations that arrived since the planner was created,
            // while retaining in-flight reservations to prevent concurrent bursts.
            _plannedDsp = Math.Max(_plannedDsp, confirmedDsp);
            _plannedClicks = Math.Max(_plannedClicks, confirmedClicks);
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
