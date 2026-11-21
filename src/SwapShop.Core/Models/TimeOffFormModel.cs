using System.ComponentModel.DataAnnotations;

namespace SwapShop.Core.Models;

public class TimeOffFormModel
{
    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }
}
