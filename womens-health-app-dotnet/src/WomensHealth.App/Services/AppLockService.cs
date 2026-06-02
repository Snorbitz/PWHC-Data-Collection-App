using System.Runtime.InteropServices;
using System.Threading;

namespace WomensHealth.App.Services;

public sealed class AppLockService : IDisposable
{
    private readonly AppPaths _paths;
    private Mutex? _mutex;
    private bool _ownsMutex;

    public AppLockService(AppPaths paths)
    {
        _paths = paths;
    }

    public bool TryAcquire()
    {
        var userInfo = $"{Environment.UserName} on {Environment.MachineName}";

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _mutex = new Mutex(initiallyOwned: true, "Global\\WomensHealthAppLocal8080", out _ownsMutex);
                if (!_ownsMutex)
                {
                    return false;
                }
            }

            File.WriteAllText(_paths.LockInfoPath, userInfo);
            return true;
        }
        catch
        {
            return true;
        }
    }

    public string ReadCurrentOwner()
    {
        try
        {
            return File.Exists(_paths.LockInfoPath) ? File.ReadAllText(_paths.LockInfoPath).Trim() : "Another user";
        }
        catch
        {
            return "Another user";
        }
    }

    public void Dispose()
    {
        if (_ownsMutex)
        {
            _mutex?.ReleaseMutex();
        }

        _mutex?.Dispose();
    }
}
