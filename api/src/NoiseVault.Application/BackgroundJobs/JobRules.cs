namespace NoiseVault.Application.BackgroundJobs
{
    public static class JobRules
    {
        public static readonly HashSet<string> NotTriggerable = new(StringComparer.Ordinal)
        {
            "events:remind-tomorrow",
        };

        public static readonly HashSet<string> RunningStates = new(StringComparer.Ordinal)
        {
            "Enqueued",
            "Processing",
        };
    }
}
