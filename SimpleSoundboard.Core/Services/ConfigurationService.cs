using SimpleSoundboard.Core.Models;
using System.Text.Json;

namespace SimpleSoundboard.Core.Services;

public class ConfigurationService
{
    private readonly string _configFilePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConfigurationService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "SimpleSoundboard");
        Directory.CreateDirectory(appFolder);

        _configFilePath = Path.Combine(appFolder, "config.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<AppConfiguration> LoadConfigurationAsync()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                return AppConfiguration.GetDefault();
            }

            var json = await File.ReadAllTextAsync(_configFilePath);
            var config = JsonSerializer.Deserialize<AppConfiguration>(json, _jsonOptions);
            return config ?? AppConfiguration.GetDefault();
        }
        catch (Exception)
        {
            return AppConfiguration.GetDefault();
        }
    }

    public async Task<bool> SaveConfigurationAsync(AppConfiguration configuration)
    {
        try
        {
            var json = JsonSerializer.Serialize(configuration, _jsonOptions);
            await File.WriteAllTextAsync(_configFilePath, json);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public string GetConfigFilePath() => _configFilePath;
}
