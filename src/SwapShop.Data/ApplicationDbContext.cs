using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SwapShop.Core.Entities;

namespace SwapShop.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Shift> Shifts => Set<Shift>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Shift>(entity =>
        {
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(s => s.AssignedUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(s => s.Date);
            entity.Property(s => s.Notes).HasMaxLength(500);
        });
    }
}