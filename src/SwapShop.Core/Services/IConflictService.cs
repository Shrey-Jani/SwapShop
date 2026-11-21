using SwapShop.Core.Models;

namespace SwapShop.Core.Services;

public interface IConflictService
{
    Task<IReadOnlyList<Conflict>> CheckAsync(string? userId, ShiftSlot slot);

    Task<IReadOnlyDictionary<int, IReadOnlyList<Conflict>>> GetWeekConflictsAsync(DateOnly weekStart);
}
