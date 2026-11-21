namespace SwapShop.Core.Models;

public record DateRange(DateOnly Start, DateOnly End)
{
    public bool Overlaps(DateRange other) => Start <= other.End && End >= other.Start;
}
