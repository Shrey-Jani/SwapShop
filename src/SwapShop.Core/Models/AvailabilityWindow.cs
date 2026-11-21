namespace SwapShop.Core.Models;

public record AvailabilityWindow(DayOfWeek Day, TimeOnly Start, TimeOnly End);
