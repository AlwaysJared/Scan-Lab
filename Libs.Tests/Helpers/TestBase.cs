using NUnit.Framework;
using System;
using System.IO;

namespace Libs.Tests.Helpers
{
    /// <summary>
    /// Base class for tests that provides common setup and teardown
    /// </summary>
    public abstract class TestBase
    {
        protected TestDatabaseFixture DbFixture { get; private set; }
        protected string TestDirectory { get; private set; }

        [SetUp]
        public virtual void SetUp()
        {
            // Create fresh database for each test
            DbFixture = new TestDatabaseFixture();

            // Create temporary directory for file system tests
            TestDirectory = FileSystemTestHelper.CreateTempTestDirectory();
        }

        [TearDown]
        public virtual void TearDown()
        {
            // Clean up database
            DbFixture?.Dispose();

            // Clean up file system
            FileSystemTestHelper.CleanupTestDirectory(TestDirectory);
        }
    }

    /// <summary>
    /// Base class for tests that only need database (no file system)
    /// </summary>
    public abstract class DatabaseTestBase
    {
        protected TestDatabaseFixture DbFixture { get; private set; }

        [SetUp]
        public virtual void SetUp()
        {
            DbFixture = new TestDatabaseFixture();
        }

        [TearDown]
        public virtual void TearDown()
        {
            DbFixture?.Dispose();
        }
    }

    /// <summary>
    /// Base class for tests that only need file system (no database)
    /// </summary>
    public abstract class FileSystemTestBase
    {
        protected string TestDirectory { get; private set; }

        [SetUp]
        public virtual void SetUp()
        {
            TestDirectory = FileSystemTestHelper.CreateTempTestDirectory();
        }

        [TearDown]
        public virtual void TearDown()
        {
            FileSystemTestHelper.CleanupTestDirectory(TestDirectory);
        }
    }
}
