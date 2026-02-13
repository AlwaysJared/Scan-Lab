using Libs.Tests.Helpers;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;

namespace Libs.Tests
{
    /// <summary>
    /// Basic smoke tests to verify test infrastructure is working
    /// Run these first to ensure PostgreSQL and file system access are configured correctly
    /// </summary>
    [TestFixture]
    [Category("Smoke")]
    public class SmokeTests : TestBase
    {
        [Test]
        public void DatabaseConnection_Succeeds()
        {
            // Verify we can connect to test PostgreSQL
            var canConnect = DbFixture.Context.Database.CanConnect();

            Assert.That(canConnect, Is.True,
                "Cannot connect to PostgreSQL. Ensure postgres is running on localhost:5432 " +
                "with user 'postgres' and password 'changeme'");
        }

        [Test]
        public async Task DatabaseContext_CanSaveAndRetrieve()
        {
            // Verify basic EF Core operations work
            var scanner = TestDataBuilder.CreateTestScanner();

            DbFixture.Context.Scanners.Add(scanner);
            await DbFixture.Context.SaveChangesAsync();

            var retrieved = await DbFixture.Context.Scanners.FindAsync(scanner.Id);

            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved.ScannerName, Is.EqualTo("Test Scanner"));
        }

        [Test]
        public void FileSystem_CanCreateDirectory()
        {
            // Verify we can create test directories
            Assert.That(Directory.Exists(TestDirectory), Is.True,
                "Test directory should be created in SetUp");

            var subDir = Path.Combine(TestDirectory, "test_subdir");
            Directory.CreateDirectory(subDir);

            Assert.That(Directory.Exists(subDir), Is.True);
        }

        [Test]
        public void FileSystem_CanCreateFiles()
        {
            // Verify we can create test files
            var testFile = FileSystemTestHelper.CreateTestFile(
                TestDirectory,
                "test.txt",
                "Hello from smoke test");

            Assert.That(File.Exists(testFile), Is.True);
            Assert.That(File.ReadAllText(testFile), Does.Contain("smoke test"));
        }

        [Test]
        public void TestDataBuilder_CreatesValidObjects()
        {
            // Verify test data builders work
            var customer = TestDataBuilder.CreateTestCustomer();
            var scanner = TestDataBuilder.CreateTestScanner();
            var order = TestDataBuilder.CreateTestOrder(scanner: scanner, customer: customer);
            var roll = TestDataBuilder.CreateTestRoll(order: order);

            Assert.That(customer, Is.Not.Null);
            Assert.That(scanner, Is.Not.Null);
            Assert.That(order.Scanner, Is.EqualTo(scanner));
            Assert.That(order.Customer, Is.EqualTo(customer));
            Assert.That(roll.Order, Is.EqualTo(order));

            // Verify profile/config builders
            var profile = TestDataBuilder.CreateTestProfile();
            var config = TestDataBuilder.CreateTestProfileConfig(profile);
            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.ProfileName, Is.EqualTo("Test Profile"));
            Assert.That(config.Profile, Is.EqualTo(profile));

            // Verify scanner with profile
            var scannerWithProfile = TestDataBuilder.CreateTestScanner(profile: profile);
            Assert.That(scannerWithProfile.Profile, Is.EqualTo(profile));
            Assert.That(scannerWithProfile.ProfileId, Is.EqualTo(profile.Id));
        }
    }
}
