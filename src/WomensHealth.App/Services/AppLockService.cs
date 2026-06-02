using System.Runtime.InteropServices;
using System.Threading;

namespace WomensHealth.App.Services;

public sealed class AppLockService : IDisposable
{
    private readonly AppPaths _paths;
    private Mutex? _mutex;
    private bool _ownsMutex;
    private bool _disposed;

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
        if (_disposed) return;
        _disposed = true;

        if (_ownsMutex && _mutex != null)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // Mutexes are thread-bound. If Dispose is called from a different thread
                // (e.g. during host shutdown), ReleaseMutex will throw. The OS will
                // automatically release the Mutex when the process terminates.
            }
            catch (ObjectDisposedException)
            {
                // SafeHandle already closed.
            }
            _ownsMutex = false;
        }

        try
        {
            _mutex?.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // Already disposed.
        }
        _mutex = null;
    }
}
