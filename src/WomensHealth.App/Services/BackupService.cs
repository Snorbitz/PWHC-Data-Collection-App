namespace WomensHealth.App.Services;

public sealed class BackupService
{
    private readonly AppPaths _paths;

    public BackupService(AppPaths paths)
    {
        _paths = paths;
    }

    public string? BackupNow(DateTime? timestamp = null)
    {
        if (!File.Exists(_paths.DatabasePath))
        {
            return null;
        }

        Directory.CreateDirectory(_paths.BackupDirectory);
        var stamp = (timestamp ?? DateTime.Now).ToString("yyyyMMdd_HHmmss");
        var destination = Path.Combine(_paths.BackupDirectory, $"womenshealth_backup_{stamp}.db");
        File.Copy(_paths.DatabasePath, destination, overwrite: true);
        RetainLatestFive();
        return destination;
    }

    private void RetainLatestFive()
    {
        var backups = Directory
            .EnumerateFiles(_paths.BackupDirectory, "womenshealth_backup_*.db")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var oldBackup in backups.Take(Math.Max(0, backups.Length - 5)))
        {
            try
            {
                File.Delete(oldBackup);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
