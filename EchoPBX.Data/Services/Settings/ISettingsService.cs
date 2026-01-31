namespace EchoPBX.Data.Services.Settings;

/// <summary>
/// Service for managing system settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Initializes the settings service by loading settings from the database.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Gets a system setting by key.
    /// </summary>
    /// <param name="key">The setting key.</param>
    string Get(string key);

    /// <summary>
    /// Sets a system setting by key.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The setting value.</param>
    Task Set(string key, string value);

    /// <summary>
    /// Sets multiple system settings.
    /// </summary>
    /// <param name="settings">The settings to set.</param>
    Task Set(IDictionary<string, string> settings);
}