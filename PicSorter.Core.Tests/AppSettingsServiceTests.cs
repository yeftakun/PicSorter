using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PicSorter.Core.Models;
using PicSorter.Core.Services;
using Xunit;

namespace PicSorter.Core.Tests
{
    public class AppSettingsServiceTests : IDisposable
    {
        private readonly string _tempFile;

        public AppSettingsServiceTests()
        {
            _tempFile = Path.Combine(Path.GetTempPath(), "PicSorter_SettingsTest_" + Guid.NewGuid() + ".json");
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(_tempFile))
                {
                    File.Delete(_tempFile);
                }
            }
            catch { }
        }

        [Fact]
        public async Task LoadSettings_FileNotExists_ReturnsDefaultSettings()
        {
            var service = new AppSettingsService(_tempFile);
            var settings = await service.LoadSettingsAsync();

            Assert.NotNull(settings);
            Assert.Equal("Copy", settings.LastUsedMode);
            Assert.Equal("Auto", settings.ThemePreference);
            Assert.Empty(settings.FavoriteDestinations);
        }

        [Fact]
        public async Task SaveAndLoad_PersistsDataCorrectly()
        {
            var service = new AppSettingsService(_tempFile);
            var settings = new AppSettings
            {
                LastUsedMode = "Move",
                ThemePreference = "Dark",
                WindowWidth = 800,
                WindowHeight = 600,
                WindowTop = 100,
                WindowLeft = 200
            };
            settings.FavoriteDestinations.Add(new DestinationFolderInfo { Shortcut = "1", FolderPath = @"C:\Test" });

            await service.SaveSettingsAsync(settings);

            // Re-load with a new instance to ensure it reads from file
            var loadedService = new AppSettingsService(_tempFile);
            var loadedSettings = await loadedService.LoadSettingsAsync();

            Assert.Equal("Move", loadedSettings.LastUsedMode);
            Assert.Equal("Dark", loadedSettings.ThemePreference);
            Assert.Equal(800, loadedSettings.WindowWidth);
            Assert.Equal(600, loadedSettings.WindowHeight);
            Assert.Equal(100, loadedSettings.WindowTop);
            Assert.Equal(200, loadedSettings.WindowLeft);
            
            Assert.Single(loadedSettings.FavoriteDestinations);
            Assert.Equal("1", loadedSettings.FavoriteDestinations.First().Shortcut);
            Assert.Equal(@"C:\Test", loadedSettings.FavoriteDestinations.First().FolderPath);
        }
    }
}
