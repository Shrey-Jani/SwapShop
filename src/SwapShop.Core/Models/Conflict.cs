using SwapShop.Core.Enums;

namespace SwapShop.Core.Models;

public record Conflict(ConflictType Type, string Message, int? RelatedShiftId = null);
