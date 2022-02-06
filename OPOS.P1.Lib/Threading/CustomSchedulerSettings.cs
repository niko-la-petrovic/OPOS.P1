namespace OPOS.P1.Lib.Threading
{
    public record CustomSchedulerSettings
    {
        public int MaxCores { get; init; }
        public int MaxConcurrentTasks { get; init; }
    }
}
