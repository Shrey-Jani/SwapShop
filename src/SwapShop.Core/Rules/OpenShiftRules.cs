using SwapShop.Core.Enums;
using SwapShop.Core.Models;

namespace SwapShop.Core.Rules;

public static class OpenShiftRules
{
    public static IReadOnlyList<string> CanClaim(
        bool employeeIsActive,
        Position employeePosition,
        ShiftDto shift,
        DateTime now)
    {
        if (!shift.IsPublished)
        {
            return ["This shift is not available."];
        }

        var errors = new List<string>();

        if (!employeeIsActive)
        {
            errors.Add("Your account is not active.");
        }

        if (shift.AssignedUserId is not null)
        {
            errors.Add("This shift has already been taken.");
        }

        if (shift.Position != employeePosition)
        {
            errors.Add("This shift is for a different position.");
        }

        if (shift.Span.Start <= now)
        {
            errors.Add("This shift has already started.");
        }

        return errors;
    }
}
