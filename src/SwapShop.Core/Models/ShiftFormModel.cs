using System.ComponentModel.DataAnnotations;
using SwapShop.Core.Enums;

namespace SwapShop.Core.Models;

public class ShiftFormModel
{
    public int? Id {get; set;}

    public DateOnly Date {get; set;} = DateOnly.FromDateTime(DateTime.Now);

    public TimeOnly StartTime {get; set;} = new(9,0);

    public TimeOnly EndTime {get; set;} = new(17,0);

    public Position Position {get; set;}

    public string? AssignedUserId {get; set;}

        [StringLength(500)]
    public string? Notes { get; set; }
}