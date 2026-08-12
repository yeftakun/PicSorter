using System.Windows;
using PicSorter.Core.Services;
using Wpf.Ui.Appearance;

namespace PicSorter.Wpf;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settingsService = new AppSettingsService();
        var settings = await settingsService.LoadSettingsAsync();

        if (settings.ThemePreference == "Light")
            ApplicationThemeManager.Apply(ApplicationTheme.Light);
        else if (settings.ThemePreference == "Dark")
            ApplicationThemeManager.Apply(ApplicationTheme.Dark);
        else
            ApplicationThemeManager.ApplySystemTheme();

        var mainWindow = new MainWindow(settings, settingsService);
        mainWindow.Show();
    }
}
