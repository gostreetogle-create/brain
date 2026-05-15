using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using Brain.Desktop.ViewModels;

namespace Brain.Desktop;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private Window? _splash;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ShowSplash("Загрузка...");

        // Инициализация в фоне, но окна создаём на UI потоке
        Task.Run(() =>
        {
            try
            {
                var vm = new MainViewModel();
                Dispatcher.Invoke(() =>
                {
                    _mainWindow = new MainWindow(vm);
                    CloseSplash();
                    _mainWindow.Show();
                    SetupTray();
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    CloseSplash();
                    MessageBox.Show($"Ошибка: {ex.Message}", "BRAIN",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        });
    }

    private void ShowSplash(string message)
    {
        _splash = new Window
        {
            Width = 380, Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.None,
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 46)),
            Topmost = true,
            WindowState = WindowState.Normal,
            ShowInTaskbar = false
        };
        var stack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        stack.Children.Add(new TextBlock
        {
            Text = "BRAIN",
            FontSize = 36, FontWeight = FontWeights.Bold,
            Foreground = Brushes.Cyan,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        stack.Children.Add(new TextBlock
        {
            Text = message,
            FontSize = 14, Foreground = Brushes.Gray,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 12, 0, 0)
        });
        stack.Children.Add(new ProgressBar
        {
            Width = 200, Height = 4,
            IsIndeterminate = true,
            Margin = new Thickness(0, 16, 0, 0)
        });
        _splash.Content = stack;
        _splash.Show();
    }

    private void CloseSplash()
    {
        if (_splash != null)
        {
            _splash.Close();
            _splash = null;
        }
    }

    private void SetupTray()
    {
        _trayIcon = new TaskbarIcon
        {
            Icon = CreateIcon(),
            ToolTipText = "BRAIN — Цифровой Сотрудник",
            Visibility = Visibility.Visible
        };

        var menu = new ContextMenu();
        menu.Background = Brushes.Transparent;

        var showItem = new MenuItem { Header = "📂 Показать окно", FontSize = 13 };
        showItem.Click += (_, _) => ShowMainWindow();
        menu.Items.Add(showItem);

        var watchItem = new MenuItem { Header = "👀 Слежение: запустить", FontSize = 13 };
        watchItem.Click += (_, _) =>
        {
            if (_mainWindow?.ViewModel.Dashboard.ToggleWatchCommand.CanExecute(null) == true)
                _mainWindow.ViewModel.Dashboard.ToggleWatchCommand.Execute(null);
            watchItem.Header = _mainWindow?.ViewModel.Dashboard.IsWatching == true
                ? "👀 Слежение: остановить" : "👀 Слежение: запустить";
        };
        menu.Items.Add(watchItem);

        menu.Items.Add(new Separator());

        var exitItem = new MenuItem { Header = "🚪 Выход", FontSize = 13 };
        exitItem.Click += (_, _) => Shutdown();
        menu.Items.Add(exitItem);

        _trayIcon.ContextMenu = menu;
        _trayIcon.TrayMouseDoubleClick += (_, _) => ShowMainWindow();
    }

    private void ShowMainWindow()
    {
        if (_mainWindow == null) return;
        _mainWindow.Show();
        _mainWindow.Activate();
        _mainWindow.WindowState = WindowState.Normal;
    }

    private static System.Drawing.Icon CreateIcon()
    {
        var icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "brain.ico");
        if (File.Exists(icoPath))
        {
            try { return new System.Drawing.Icon(icoPath); }
            catch { }
        }
        using var bmp = new System.Drawing.Bitmap(32, 32);
        using var g = System.Drawing.Graphics.FromImage(bmp);
        g.Clear(System.Drawing.Color.Transparent);
        g.FillEllipse(new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(137, 180, 250)), 2, 2, 28, 28);
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
