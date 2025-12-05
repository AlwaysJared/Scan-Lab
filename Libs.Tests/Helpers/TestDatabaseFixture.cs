using Libs.Data.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Libs.Tests.Helpers
{
    /// <summary>
    /// Provides a test PostgreSQL database instance for integration tests.
    /// Each test gets a fresh database with migrations applied.
    /// </summary>
    public class TestDatabaseFixture : IDisposable
    {
        private const string TestConnectionString = "Host=localhost;Database=ScanLabTest;Username=postgres;Password=changeme";
        private readonly string _databaseName;

        public ScanLabContext Context { get; private set; }

        public TestDatabaseFixture()
        {
            // Create unique database name for this test run
            _databaseName = $"ScanLabTest_{Guid.NewGuid():N}";

            var optionsBuilder = new DbContextOptionsBuilder<ScanLabContext>();
            optionsBuilder.UseNpgsql(TestConnectionString.Replace("ScanLabTest", _databaseName));

            Context = new ScanLabContext(optionsBuilder.Options);

            // Create database and apply migrations
            Context.Database.EnsureDeleted(); // Clean start
            Context.Database.EnsureCreated();  // Create schema
        }

        /// <summary>
        /// Creates a new context instance for testing
        /// </summary>
        public ScanLabContext CreateContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ScanLabContext>();
            optionsBuilder.UseNpgsql(TestConnectionString.Replace("ScanLabTest", _databaseName));
            return new ScanLabContext(optionsBuilder.Options);
        }

        /// <summary>
        /// Resets the database to clean state (clears all data)
        /// </summary>
        public async Task ResetDatabase()
        {
            await Context.Database.EnsureDeletedAsync();
            await Context.Database.EnsureCreatedAsync();
        }

        public void Dispose()
        {
            // Clean up test database
            Context.Database.EnsureDeleted();
            Context.Dispose();
        }
    }
}
