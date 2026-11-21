using SwapShop.Core.Enums;
using SwapShop.Core.Models;
using SwapShop.Core.Rules;

namespace SwapShop.Tests;

public class AvailabilityResolverTests
{
    private static AvailabilityWindow Window(DayOfWeek day, int startHour, int endHour) =>
        new(day, new TimeOnly(startHour, 0), new TimeOnly(endHour, 0));

    [Fact]
    public void NothingSet_ReturnsNone()
    {
        var result = AvailabilityResolver.Resolve([], []);

        Assert.Equal(AvailabilitySource.None, result.Source);
        Assert.Empty(result.Windows);
    }

    [Fact]
    public void OnlyStanding_ReturnsStanding()
    {
        var standing = new[] { Window(DayOfWeek.Monday, 9, 17) };

        var result = AvailabilityResolver.Resolve(standing, []);

        Assert.Equal(AvailabilitySource.Standing, result.Source);
        Assert.Equal(standing, result.Windows);
    }

    [Fact]
    public void OnlyCustom_ReturnsCustom()
    {
        var custom = new[] { Window(DayOfWeek.Tuesday, 10, 14) };

        var result = AvailabilityResolver.Resolve([], custom);

        Assert.Equal(AvailabilitySource.Custom, result.Source);
        Assert.Equal(custom, result.Windows);
    }

    [Fact]
    public void Custom_ReplacesStandingCompletely()
    {
        var standing = new[]
        {
            Window(DayOfWeek.Monday, 9, 17),
            Window(DayOfWeek.Tuesday, 9, 17),
            Window(DayOfWeek.Wednesday, 9, 17)
        };
        var custom = new[] { Window(DayOfWeek.Monday, 12, 16) };

        var result = AvailabilityResolver.Resolve(standing, custom);

        Assert.Equal(AvailabilitySource.Custom, result.Source);
        Assert.Equal(custom, result.Windows);
    }

    [Fact]
    public void EmptyCustom_FallsBackToStanding()
    {
        var standing = new[] { Window(DayOfWeek.Friday, 16, 22) };

        var result = AvailabilityResolver.Resolve(standing, []);

        Assert.Equal(AvailabilitySource.Standing, result.Source);
        Assert.Equal(standing, result.Windows);
    }
}
