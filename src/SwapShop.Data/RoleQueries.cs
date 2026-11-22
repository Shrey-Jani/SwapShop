using Microsoft.EntityFrameworkCore;
using SwapShop.Core.Constants;

namespace SwapShop.Data;

internal static class RoleQueries
{
    public static Task<bool> IsInManagerRoleAsync(this ApplicationDbContext db, string userId) =>
        (from userRole in db.UserRoles
         join role in db.Roles on userRole.RoleId equals role.Id
         where userRole.UserId == userId && role.Name == Roles.Manager
         select userRole).AnyAsync();
}
