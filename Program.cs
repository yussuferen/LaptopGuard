using LaptopGuard.Configuration;

namespace LaptopGuard;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        var configManager = new ConfigManager();
        Application.Run(new TrayApplicationContext(configManager));
    }
}