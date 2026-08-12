using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using PicSorter.Core.Models;

namespace PicSorter.Core.Services
{
    public class AppSettingsService
    {
        private readonly string _settingsFilePath;

        public AppSettingsService()
        {
            // %AppData%/PicSorter/settings.json
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _settingsFilePath = Path.Combine(appData, "PicSorter", "settings.json");
        }

        // For testing / custom path injection
        public AppSettingsService(string customPath)
        {
            _settingsFilePath = customPath;
        }

        public async Task<AppSettings> LoadSettingsAsync()
        {
            if (!File.Exists(_settingsFilePath))
            {
                return new AppSettings();
            }

            try
            {
                string json = await File.ReadAllTextAsync(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                return settings ?? new AppSettings();
            }
            catch
            {
                // In case of parsing error, return default
                return new AppSettings();
            }
        }

        public async Task SaveSettingsAsync(AppSettings settings)
        {
            try
            {
                string? dir = Path.GetDirectoryName(_settingsFilePath);
                if (dir != null && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_settingsFilePath, json);
            }
            catch
            {
                // Ignore save errors
            }
        }
    }
}
