using WomensHealth.App.Services;

namespace WomensHealth.App.Tests;

public sealed class BackupServiceTests
{
    [Fact]
    public void BackupCreatesTimestampedFileAndKeepsLatestFive()
    {
        var directory = TestPaths.CreateTempDirectory();
        var paths = TestPaths.CreateAppPaths(directory);
        File.WriteAllText(paths.DatabasePath, "db");
        var service = new BackupService(paths);

        for (var i = 0; i < 7; i++)
        {
            service.BackupNow(new DateTime(2026, 6, 2, 12, 0, i));
        }

        var backups = Directory.EnumerateFiles(paths.BackupDirectory, "womenshealth_backup_*.db")
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(5, backups.Length);
        Assert.DoesNotContain("womenshealth_backup_20260602_120000.db", backups);
        Assert.Contains("womenshealth_backup_20260602_120006.db", backups);
    }
}
