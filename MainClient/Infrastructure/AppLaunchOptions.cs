namespace MainClient.Infrastructure
{
    public sealed class AppLaunchOptions
    {
        public const string AutoStartArgument = "--auto-start";

        public bool AutoStartTasks { get; init; }
    }
}
