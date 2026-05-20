using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace MascotApp;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _trayIcon = (TaskbarIcon)FindResource("TrayIcon");

        var mainWindow = new MainWindow();
        mainWindow.Show();
    }

    private void OnToggleVisibility(object sender, RoutedEventArgs e)
    {
        var mainWindow = (MainWindow)MainWindow;
        mainWindow.ToggleVisibility();
    }

    private void OnSettings(object sender, RoutedEventArgs e)
    {
        var mainWindow = (MainWindow)MainWindow;
        var settings = new SettingsWindow(mainWindow); // MainWindow 넘기기
        settings.Show();
    }

    private void OnExit(object sender, RoutedEventArgs e)
    {
        _trayIcon?.Dispose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}