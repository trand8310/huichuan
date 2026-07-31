

namespace MainClient.Scheduler
{
    public sealed class TrafficTaskStateEntity
    {
        public long Request;
        public long Start;
        public long DSP;
        public long Clickthrough;
        public long Success;
        public long Error;
        public long Failure;
        public long Complete;


        private long _deltaStart;
        private long _deltaDsp;
        private long _deltaClickthrough;

        public double ClickRatio => DSP == 0 ? 0 : (double)Clickthrough / DSP;

        public TrafficTaskUiSnapshot ToUiSnapshot(int taskId)
        {
            var request = Interlocked.Read(ref Request);
            var start = Interlocked.Read(ref Start);
            var dsp = Interlocked.Read(ref DSP);
            var clickthrough = Interlocked.Read(ref Clickthrough);
            var success = Interlocked.Read(ref Success);
            var error = Interlocked.Read(ref Error);
            var failure = Interlocked.Read(ref Failure);
            var complete = Interlocked.Read(ref Complete);

            return new TrafficTaskUiSnapshot(
                taskId,
                request,
                start,
                dsp,
                clickthrough,
                success,
                error,
                failure,
                complete,
                dsp == 0 ? 0 : clickthrough / (double)dsp);
        }

        public void Add(TrafficTaskStateKind type, int count)
        {
            switch (type)
            {
                case TrafficTaskStateKind.Request:
                    Interlocked.Add(ref Request, count);
                    break;

                case TrafficTaskStateKind.Start:
                    Interlocked.Add(ref Start, count);
                    Interlocked.Add(ref _deltaStart, count);
                    break;

                case TrafficTaskStateKind.DSP:
                    Interlocked.Add(ref DSP, count);
                    Interlocked.Add(ref _deltaDsp, count);
                    break;

                case TrafficTaskStateKind.Clickthrough:
                    Interlocked.Add(ref Clickthrough, count);
                    Interlocked.Add(ref _deltaClickthrough, count);
                    break;

                case TrafficTaskStateKind.Success:
                    Interlocked.Add(ref Success, count);
                    break;

                case TrafficTaskStateKind.Error:
                    Interlocked.Add(ref Error, count);
                    break;

                case TrafficTaskStateKind.Failure:
                    Interlocked.Add(ref Failure, count);
                    break;

                case TrafficTaskStateKind.Complete:
                    Interlocked.Add(ref Complete, count);
                    break;
            }
        }

        public TrafficTaskStateSnapshot GetSnapshot()
        {
            return new TrafficTaskStateSnapshot(
                Start: Interlocked.Read(ref _deltaStart),
                Dsp: Interlocked.Read(ref _deltaDsp),
                Click: Interlocked.Read(ref _deltaClickthrough)
            );
        }

        public void Commit(TrafficTaskStateSnapshot delta)
        {
            if (delta.Start != 0) Interlocked.Add(ref _deltaStart, -delta.Start);
            if (delta.Dsp != 0) Interlocked.Add(ref _deltaDsp, -delta.Dsp);
            if (delta.Click != 0) Interlocked.Add(ref _deltaClickthrough, -delta.Click);
        }

        public Dictionary<string, long> ToMetricDictionary(TrafficTaskStateSnapshot delta)
        {
            var dict = new Dictionary<string, long>(3);
            if (delta.Start > 0) dict["start"] = delta.Start;
            if (delta.Dsp > 0) dict["dsp"] = delta.Dsp;
            if (delta.Click > 0) dict["click"] = delta.Click;
            return dict;
        }
    }
}
