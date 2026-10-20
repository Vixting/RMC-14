using Robust.Client.Timing;

namespace Content.Client._RMC14.Timing;

public static class RMCClientTimingExtensions
{
    public static TimeSpan LastRealTime(this IClientGameTiming timing)
    {
        var predictedTicks = Math.Max(0, (int) (timing.CurTick.Value - timing.LastRealTick.Value));
        return timing.CurTime - timing.TickPeriod * predictedTicks;
    }
}
