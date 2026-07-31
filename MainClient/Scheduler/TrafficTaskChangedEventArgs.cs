namespace MainClient.Scheduler
{
    public enum TrafficTaskStateKind
    {
        Request = 0,
        Start = 1,
        DSP = 2,
        Clickthrough =3,
        Success = 4,
        Complete = 5,
        Error = 6,
        Failure = 7,
        X5Sec = 8,
    }
    public static class AdTrafficTaskExtensions
    {
        public static string FullName(this TrafficTaskStateKind type) => type switch
        {
            TrafficTaskStateKind.Request => "request",
            TrafficTaskStateKind.Start => "start",
            TrafficTaskStateKind.DSP => "dsp",
            TrafficTaskStateKind.Clickthrough => "click",
            TrafficTaskStateKind.Success => "success",
            TrafficTaskStateKind.Complete => "complete",
            TrafficTaskStateKind.Error => "error",
            TrafficTaskStateKind.Failure => "failure",
            TrafficTaskStateKind.X5Sec => "x5sec",
            _ => "unknown"
        };
    }
    public class TrafficTaskChangedEventArgs : EventArgs
    {
        public TrafficTaskStateKind Kind { get; set; }
        public int Id { get; }
        public int Count { get; }
        public string? Data { get; set; }
        public TrafficTaskChangedEventArgs(TrafficTaskStateKind kind,int id, int count,string? data = null)
        {
            Kind = kind;
            Id = id;
            Count = count;
            Data = data;
        }
    }
}
