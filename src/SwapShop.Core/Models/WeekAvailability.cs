using SwapShop.Core.Enums;

namespace SwapShop.Core.Models;

public record WeekAvailability(AvailabilitySource Source, IReadOnlyList<AvailabilityWindow> Windows);
