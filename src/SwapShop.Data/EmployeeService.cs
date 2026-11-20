using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SwapShop.Core.Constants;
using SwapShop.Core.Models;
using SwapShop.Core.Services;

namespace SwapShop.Data;

public class EmployeeService(UserManager<ApplicationUser> userManager) : IEmployeeService
{
    public async Task<IReadOnlyList<EmployeeDto>> GetAllAsync(bool includeInactive = true)
    {
        var query = userManager.Users.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(u => u.IsActive);
        }


        return await query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Select(u => new EmployeeDto(
                u.Id, u.Email ?? string.Empty, u.FirstName, u.LastName, u.Position, u.HourlyRate, u.IsActive))
            .ToListAsync();
    }

    public async Task<EmployeeFormModel?> GetForEditAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id);

        if (user == null)
        {
            return null;
        }

        return new EmployeeFormModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Position = user.Position,
            HourlyRate = user.HourlyRate,
        };
    }

    public async Task<ServiceResult> CreateAsync(EmployeeFormModel model)
    {
        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Position = model.Position,
            HourlyRate = model.HourlyRate,
            IsActive = true
        };

        var created = await userManager.CreateAsync(user, model.Password);
        if (!created.Succeeded)
        {
            return FormIdentity(created);
        }
        
        var roleAdded = await userManager.AddToRoleAsync(user, Roles.Staff);
        if(!roleAdded.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return FormIdentity(roleAdded);
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UpdateAsync(EmployeeFormModel model)
    {
        if (model.Id is null)
        {
            return ServiceResult.Failure("Employee id is missing.");
        }

        var user = await userManager.FindByIdAsync(model.Id);
        if(user is null)
        {
            return ServiceResult.Failure("Employee not found.");
        }

        if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailResult = await userManager.SetEmailAsync(user, model.Email);
            if (!emailResult.Succeeded)
            {
                return FormIdentity(emailResult);
            }

            var nameResult = await userManager.SetUserNameAsync(user, model.Email);
            if (!nameResult.Succeeded)
            {
                return FormIdentity(nameResult);
            } 
        }

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Position = model.Position;
        user.HourlyRate = model.HourlyRate;
        user.EmailConfirmed = true;

        return FormIdentity(await userManager.UpdateAsync(user));
    }

    public async Task<ServiceResult> SetActiveAsync(string id, bool isActive)
    {
        var user = await userManager.FindByIdAsync(id);
        if(user is null)
        {
            return ServiceResult.Failure("Employee not found.");
        }

        user.IsActive = isActive;
        return FormIdentity(await userManager.UpdateAsync(user));
    }

    private static ServiceResult FormIdentity(IdentityResult result) =>
        result.Succeeded 
            ? ServiceResult.Success() 
            : ServiceResult.Failure(result.Errors.Select(e => e.Description));
}