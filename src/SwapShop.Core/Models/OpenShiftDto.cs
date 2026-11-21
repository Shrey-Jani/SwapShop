namespace SwapShop.Core.Models;

public record OpenShiftDto(ShiftDto Shift, IReadOnlyList<Conflict> Conflicts)
{
    public bool CanTake => Conflicts.Count == 0;
}
