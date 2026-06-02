namespace WomensHealth.App;

public sealed class AppPaths
{
    public AppPaths(IWebHostEnvironment environment, IConfiguration configuration)
    {
        AppDirectory = configuration["WomensHealth:AppDirectory"] ?? environment.ContentRootPath;
        WebRootDirectory = configuration["WomensHealth:WebRootDirectory"]
            ?? environment.WebRootPath
            ?? Path.Combine(AppDirectory, "wwwroot");
        DatabasePath = configuration["WomensHealth:DatabasePath"] ?? Path.Combine(AppDirectory, "womenshealth.db");
        LogPath = configuration["WomensHealth:LogPath"] ?? Path.Combine(AppDirectory, "server.log");
        BackupDirectory = configuration["WomensHealth:BackupDirectory"] ?? Path.Combine(AppDirectory, "backups");
        LockInfoPath = configuration["WomensHealth:LockInfoPath"] ?? Path.Combine(AppDirectory, "server.info");
        DataFormPath = Path.Combine(WebRootDirectory, "WomensHealth_DataForm.html");
        ViewerPath = Path.Combine(WebRootDirectory, "WomensHealth_Viewer.html");
        OptionsPath = Path.Combine(WebRootDirectory, "data.json");
    }

    public string AppDirectory { get; }
    public string WebRootDirectory { get; }
    public string DatabasePath { get; }
    public string LogPath { get; }
    public string BackupDirectory { get; }
    public string LockInfoPath { get; }
    public string DataFormPath { get; }
    public string ViewerPath { get; }
    public string OptionsPath { get; }
}
