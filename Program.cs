using LaptopGuard.Configuration;
using LaptopGuard.Services;

namespace LaptopGuard;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        SleepPreventionService.RestoreIfRecovering();
        var configManager = new ConfigManager();
        Application.Run(new TrayApplicationContext(configManager));
    }
}