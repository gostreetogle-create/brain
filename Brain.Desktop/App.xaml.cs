using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using Brain.Desktop.ViewModels;

namespace Brain.Desktop;

public partial class App : Application
{
    private static Mutex? _mutex;
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BRAIN", "brain.log");

    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Запрет второго экземпляра
        bool createdNew;
        _mutex = new Mutex(true, "BRAIN_AI_SINGLE_INSTANCE", out createdNew);
        if (!createdNew)
        {
            MessageBox.Show("BRAIN уже запущен.", "BRAIN", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        // Глобальный перехват ошибок
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            LogFatal(args.ExceptionObject as Exception, "AppDomain crash");

        DispatcherUnhandledException += (_, args) =>
        {
            LogFatal(args.Exception, "Dispatcher crash");
            args.Handled = true;
        };

        try
        {
            base.OnStartup(e);

            // Создаём папку для логов
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            Log("Запуск...");

            var splash = ShowSplash();

            // Инициализация в фоне
            Task.Run(() =>
            {
                try
                {
                    var vm = new MainViewModel();
                    Dispatcher.Invoke(() =>
                    {
                        splash.Close();
                        _mainWindow = new MainWindow(vm);
                        _mainWindow.Show();
                        SetupTray();
                        Log("Запуск успешен");
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        splash.Close();
                        LogFatal(ex, "Init error");
                    });
                }
            });
        }
        catch (Exception ex)
        {
            LogFatal(ex, "OnStartup");
        }
    }

    private Window ShowSplash()
    {
        var w = new Window
        {
            Width = 380, Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.None,
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 46)),
            Topmost = true, ShowInTaskbar = false
        };
        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        stack.Children.Add(new TextBlock { Text = "BRAIN", FontSize = 36, FontWeight = FontWeights.Bold, Foreground = Brushes.Cyan, HorizontalAlignment = HorizontalAlignment.Center });
        stack.Children.Add(new TextBlock { Text = "Загрузка...", FontSize = 14, Foreground = Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 12, 0, 0) });
        stack.Children.Add(new ProgressBar { Width = 200, Height = 4, IsIndeterminate = true, Margin = new Thickness(0, 16, 0, 0) });
        w.Content = stack;
        w.Show();
        return w;
    }

    private void SetupTray()
    {
        _trayIcon = new TaskbarIcon { Icon = CreateIcon(), ToolTipText = "BRAIN", Visibility = Visibility.Visible };
        var menu = new ContextMenu { Background = Brushes.Transparent };
        var show = new MenuItem { Header = "📂 Показать окно", FontSize = 13 };
        show.Click += (_, _) => ShowWindow();
        menu.Items.Add(show);
        menu.Items.Add(new Separator());
        var exit = new MenuItem { Header = "🚪 Выход", FontSize = 13 };
        exit.Click += (_, _) =>
        {
            _trayIcon?.Dispose();
            Environment.Exit(0);
        };
        menu.Items.Add(exit);
        _trayIcon.ContextMenu = menu;
        _trayIcon.TrayMouseDoubleClick += (_, _) => ShowWindow();
    }

    private void ShowWindow()
    {
        if (_mainWindow == null) return;
        _mainWindow.Show();
        _mainWindow.Activate();
        _mainWindow.WindowState = WindowState.Normal;
    }

    private static void Log(string msg)
    {
        try { File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] {msg}\n"); }
        catch { }
    }

    private void LogFatal(Exception? ex, string source)
    {
        var msg = $"=== {source} ===\n{ex?.Message}\n{ex?.StackTrace}";
        if (ex?.InnerException != null)
            msg += $"\nInner: {ex.InnerException.Message}";
        Log(msg);
        try { MessageBox.Show($"Ошибка: {ex?.Message}\n\nПодробности: {LogPath}", "BRAIN", MessageBoxButton.OK, MessageBoxImage.Error); }
        catch { }
        Environment.Exit(1);
    }

    private static System.Drawing.Icon CreateIcon()
    {
        var ico = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "brain.ico");
        if (File.Exists(ico)) try { return new System.Drawing.Icon(ico); } catch { }
        using var bmp = new System.Drawing.Bitmap(32, 32);
        using var g = System.Drawing.Graphics.FromImage(bmp);
        g.Clear(System.Drawing.Color.Transparent);
        g.FillEllipse(new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(137, 180, 250)), 2, 2, 28, 28);
        g.DrawString("B", new System.Drawing.Font("Segoe UI", 18, System.Drawing.FontStyle.Bold), System.Drawing.Brushes.White, 7, 4);
        return System.Drawing.Icon.FromHandle(bmp.GetHicon());
    }

    protected override void OnExit(ExitEventArgs e) { _trayIcon?.Dispose(); base.OnExit(e); }
}
