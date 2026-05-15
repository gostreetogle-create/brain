using System.Windows;
using Brain.Desktop.ViewModels;

namespace Brain.Desktop;

public partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow() : this(null) { }

    public MainWindow(MainViewModel? viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel ?? new MainViewModel();
        DataContext = ViewModel;

        StateChanged += OnStateChanged;
        Closing += OnClosing;
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
            Hide();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (ViewModel.Dashboard.IsWatching)
        {
            e.Cancel = true;
            Hide();
        }
    }
}
