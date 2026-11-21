using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SwapShop.Core.Entities;
using SwapShop.Core.Enums;
using SwapShop.Data;

namespace SwapShop.Tests.Infrastructure;

public sealed class TestDatabase : IDisposable
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"swapshop-test-{Guid.NewGuid():N}.db");
    private readonly DbContextOptions<ApplicationDbContext> options;

    public TestDatabase()
    {
        options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"DataSource={path}")
            .Options;

        using var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();

        Factory = new ContextFactory(options);
    }

    public IDbContextFactory<ApplicationDbContext> Factory { get; }

    public ApplicationDbContext CreateContext() => new(options);

    public void AddUser(string id, string firstName, string lastName, Position position = Position.Cook, bool active = true)
    {
        using var db = CreateContext();
        db.Users.Add(new ApplicationUser
        {
            Id = id,
            UserName = $"{id}@test.local",
            Email = $"{id}@test.local",
            FirstName = firstName,
            LastName = lastName,
            Position = position,
            IsActive = active
        });
        db.SaveChanges();
    }

    public int AddShift(
        DateOnly date,
        TimeOnly start,
        TimeOnly end,
        string? assignedUserId = null,
        Position position = Position.Cook,
        bool published = true)
    {
        using var db = CreateContext();
        var shift = new Shift
        {
            Date = date,
            StartTime = start,
            EndTime = end,
            AssignedUserId = assignedUserId,
            Position = position,
            IsPublished = published
        };
        db.Shifts.Add(shift);
        db.SaveChanges();
        return shift.Id;
    }

    public string? GetAssignedUserId(int shiftId)
    {
        using var db = CreateContext();
        return db.Shifts.AsNoTracking().Single(s => s.Id == shiftId).AssignedUserId;
    }

    public void AddAvailability(string userId, DayOfWeek day, TimeOnly start, TimeOnly end, DateOnly? weekStart = null)
    {
        using var db = CreateContext();
        db.Availabilities.Add(new Availability
        {
            UserId = userId,
            DayOfWeek = day,
            StartTime = start,
            EndTime = end,
            WeekStart = weekStart
        });
        db.SaveChanges();
    }

    public void AddTimeOff(string userId, DateOnly start, DateOnly end, TimeOffStatus status)
    {
        using var db = CreateContext();
        db.TimeOffRequests.Add(new TimeOffRequest
        {
            UserId = userId,
            StartDate = start,
            EndDate = end,
            Status = status,
            RequestedAtUtc = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        foreach (var file in new[] { path, path + "-wal", path + "-shm", path + "-journal" })
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }

    private sealed class ContextFactory(DbContextOptions<ApplicationDbContext> options)
        : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(options);
    }
}
