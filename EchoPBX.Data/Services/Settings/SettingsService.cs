using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EchoPBX.Data.Services.Settings;

public class SettingsService(IServiceProvider serviceProvider, ILogger<SettingsService> logger) : ISettingsService
{
    private readonly Dictionary<string, string> _settings = new();
    private EchoDbContext? _dbContext;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        var scope = serviceProvider.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<EchoDbContext>();

        var settings = await _dbContext.SystemSettings.Select(x => new { x.Name, x.Value }).ToArrayAsync();

        // doing this manually, so we can overwrite existing settings if there are duplicates
        foreach (var setting in settings)
        {
            if (_settings.TryGetValue(setting.Name, out var value))
            {
                logger.LogWarning("Duplicate system setting found for {SettingName}. Overwriting previous value {PreviousValue} with {NewValue}", setting.Name, value, setting.Value);
            }

            _settings[setting.Name] = setting.Value;
        }
    }

    /// <inheritdoc />
    public string Get(string key)
    {
        if (_settings.TryGetValue(key, out var value))
        {
            return value;
        }

        throw new KeyNotFoundException($"Setting with key '{key}' not found.");
    }

    /// <inheritdoc />
    public async Task Set(string key, string value)
    {
        if (_dbContext == null)
        {
            throw new InvalidOperationException("SettingsService is not initialized. Call InitializeAsync() before using.");
        }

        await _dbContext.BulkInsertOrUpdateAsync([
            new Models.SystemSetting
            {
                Name = key,
                Value = value
            }
        ]);

        _settings[key] = value;
    }

    /// <inheritdoc />
    public async Task Set(IDictionary<string, string> settings)
    {
        if (_dbContext == null)
        {
            throw new InvalidOperationException("SettingsService is not initialized. Call InitializeAsync() before using.");
        }

        await _dbContext.BulkInsertOrUpdateAsync(settings.Select(x => new Models.SystemSetting
        {
            Name = x.Key,
            Value = x.Value
        }).ToList());

        foreach (var setting in settings)
        {
            _settings[setting.Key] = setting.Value;
        }
    }
}