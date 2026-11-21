using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SwapShop.Core.Entities;

namespace SwapShop.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<Availability> Availabilities => Set<Availability>();
    public DbSet<TimeOffRequest> TimeOffRequests => Set<TimeOffRequest>();

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

        builder.Entity<Availability>(entity =>
        {
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(a => new { a.UserId, a.DayOfWeek });
        });

        builder.Entity<TimeOffRequest>(entity =>
        {
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(r => r.ReviewedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(r => r.Reason).HasMaxLength(500);

            entity.HasIndex(r => new { r.UserId, r.StartDate, r.EndDate });
        });
    }
}