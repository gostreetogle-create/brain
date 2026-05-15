using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using Brain.Desktop.ViewModels;

namespace Brain.Desktop;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _trayIcon = new TaskbarIcon
        {
            Icon = CreateIcon(),
            ToolTipText = "BRAIN — Цифровой Сотрудник",
            Visibility = Visibility.Visible
        };

        var menu = new ContextMenu();
        menu.Background = System.Windows.Media.Brushes.Transparent;

        var showItem = new MenuItem { Header = "📂 Показать окно", FontSize = 13 };
        showItem.Click += (_, _) => ShowMainWindow();
        menu.Items.Add(showItem);

        var watchItem = new MenuItem { Header = "👀 Слежение: запустить", FontSize = 13 };
        watchItem.Click += (_, _) =>
        {
            if (_mainWindow?.ViewModel.Dashboard.ToggleWatchCommand.CanExecute(null) == true)
                _mainWindow.ViewModel.Dashboard.ToggleWatchCommand.Execute(null);

            if (_mainWindow?.ViewModel.Dashboard.IsWatching == true)
                watchItem.Header = "👀 Слежение: остановить";
            else
                watchItem.Header = "👀 Слежение: запустить";
        };
        menu.Items.Add(watchItem);

        menu.Items.Add(new Separator());

        var exitItem = new MenuItem { Header = "🚪 Выход", FontSize = 13 };
        exitItem.Click += (_, _) => Shutdown();
        menu.Items.Add(exitItem);

        _trayIcon.ContextMenu = menu;
        _trayIcon.TrayMouseDoubleClick += (_, _) => ShowMainWindow();

        ShowMainWindow();
    }

    private void ShowMainWindow()
    {
        if (_mainWindow == null)
        {
            _mainWindow = new MainWindow();
            _mainWindow.Closed += (_, _) => _mainWindow = null;
        }
        _mainWindow.Show();
        _mainWindow.Activate();
        _mainWindow.WindowState = WindowState.Normal;
    }

    private static System.Drawing.Icon CreateIcon()
    {
        using var bmp = new System.Drawing.Bitmap(32, 32);
        using var g = System.Drawing.Graphics.FromImage(bmp);
        g.Clear(System.Drawing.Color.Transparent);
        using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(137, 180, 250));
        g.FillEllipse(brush, 2, 2, 28, 28);
        g.DrawString("B", new System.Drawing.Font("Segoe UI", 18, System.Drawing.FontStyle.Bold),
            System.Drawing.Brushes.White, 7, 4);
        return System.Drawing.Icon.FromHandle(bmp.GetHicon());
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
