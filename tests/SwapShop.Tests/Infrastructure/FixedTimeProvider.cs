namespace SwapShop.Tests.Infrastructure;

public sealed class FixedTimeProvider(DateTime localNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() =>
        new(DateTime.SpecifyKind(localNow, DateTimeKind.Utc));

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
