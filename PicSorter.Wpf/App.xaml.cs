using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Appearance;

namespace PicSorter.Wpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // Follow Windows system theme (Light/Dark/Auto)
        ApplicationThemeManager.ApplySystemTheme();
    }
}
