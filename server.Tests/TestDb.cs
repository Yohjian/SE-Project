using Microsoft.EntityFrameworkCore;
using QuattroLingo.Data;

namespace QuattroLingo.Tests
{
    /// <summary>Creates a fresh, isolated EF Core in-memory database for each test.</summary>
    internal static class TestDb
    {
        public static AppDbContext Create() => new(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
    }
}
