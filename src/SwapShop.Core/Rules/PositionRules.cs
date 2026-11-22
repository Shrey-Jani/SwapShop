using SwapShop.Core.Enums;

namespace SwapShop.Core.Rules;

public static class PositionRules
{
    public static bool IsManager(Position position, bool hasManagerRole) =>
        hasManagerRole || position == Position.Manager;

    public static bool CanWork(Position employeePosition, bool employeeIsManager, Position shiftPosition)
    {
        if (employeeIsManager)
        {
            return true;
        }

        if (shiftPosition == employeePosition)
        {
            return true;
        }

        return shiftPosition is not (Position.Cook or Position.Manager);
    }

    public static IReadOnlyList<Position> WorkablePositions(Position employeePosition, bool employeeIsManager) =>
        Enum.GetValues<Position>()
            .Where(p => CanWork(employeePosition, employeeIsManager, p))
            .ToList();
}
