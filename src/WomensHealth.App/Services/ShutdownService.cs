namespace WomensHealth.App.Services;

public sealed class ShutdownService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IWebHostEnvironment _environment;

    public ShutdownService(IHostApplicationLifetime lifetime, IWebHostEnvironment environment)
    {
        _lifetime = lifetime;
        _environment = environment;
    }

    public void StopAfterResponse()
    {
        if (_environment.IsEnvironment("Testing"))
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            await Task.Delay(100);
            _lifetime.StopApplication();
        });
    }
}
