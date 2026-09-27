using System.Text.Json;

namespace LaptopGuard.Configuration;

public class ConfigManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _configPath;

    public AppConfig Config { get; private set; }

    public ConfigManager(string? configPath = null)
    {
        _configPath = configPath ?? ResolveConfigPath();
        Config = Load();
    }

    private static string ResolveConfigPath()
    {
        string baseDirConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        string projectDirConfig = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "config.json"));

        if (File.Exists(projectDirConfig))
        {
            return projectDirConfig;
        }

        if (File.Exists(baseDirConfig))
        {
            return baseDirConfig;
        }

        if (File.Exists(Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "LaptopGuard.csproj"))))
        {
            return projectDirConfig;
        }

        return baseDirConfig;
    }

    public AppConfig Load()
    {
        if (!File.Exists(_configPath))
        {
            Config = new AppConfig();
            Save();
            return Config;
        }

        try
        {
            string json = File.ReadAllText(_configPath);
            Config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
        }
        catch (Exception)
        {
            Config = new AppConfig();
        }

        return Config;
    }

    public void Save()
    {
        string directory = Path.GetDirectoryName(_configPath)!;
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(Config, JsonOptions);
        File.WriteAllText(_configPath, json);
    }
}
