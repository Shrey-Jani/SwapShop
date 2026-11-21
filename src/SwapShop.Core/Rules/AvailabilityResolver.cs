using SwapShop.Core.Enums;
using SwapShop.Core.Models;

namespace SwapShop.Core.Rules;

public static class AvailabilityResolver
{
    public static WeekAvailability Resolve(
        IReadOnlyList<AvailabilityWindow> standing,
        IReadOnlyList<AvailabilityWindow> weekOverride)
    {
        if (weekOverride.Count > 0)
        {
            return new WeekAvailability(AvailabilitySource.Custom, weekOverride);
        }

        if (standing.Count > 0)
        {
            return new WeekAvailability(AvailabilitySource.Standing, standing);
        }

        return new WeekAvailability(AvailabilitySource.None, []);
    }
}
