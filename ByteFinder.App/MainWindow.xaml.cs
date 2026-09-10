using System.Diagnostics;
using ByteFinder.Core.Scanner;
using System.IO;

namespace ByteFinder.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();

        var scanner = new Win32Scanner();

        string path = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile),
            "Downloads"
        );

        var result = scanner.ScanDirectory(path);

        for (int i = 0; i < result.Entries.Count; i++)
        {
            var entry = result.Entries[i];

            Debug.WriteLine(
                $"{i} | " +
                $"Parent={entry.ParentIndex} | " +
                $"{entry.Name} | " +
                $"{entry.Size} bytes | " +
                $"Directory={entry.IsDirectory}"
            );
        }
    }

    private void TitleBar_OnMouseLeftButtonDown(
        object sender,
        System.Windows.Input.MouseButtonEventArgs e)
    {
        // This allows the user to also maximize/reshape the window by double clicking the title bar.
        if (e.ClickCount == 2)
        {
            ToggleMaximizeRestore();
            return;
        }

        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void MinimizeButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        WindowState = System.Windows.WindowState.Minimized;
    }

    private void MaximizeRestoreButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        ToggleMaximizeRestore();
    }

    private void CloseButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        Close();
    }

    // Separate function for icon change on maximize/reshape because both double clicking and the button can call it.
    private void Window_OnStateChanged(object? sender, EventArgs e)
    {
        MaximizeRestoreIcon.Text = WindowState == System.Windows.WindowState.Maximized
            ? "\uE923"  // Reshape icon
            : "\uE922"; // Maximize icon
    }

    private void ToggleMaximizeRestore()
    {
        WindowState = WindowState == System.Windows.WindowState.Maximized
            ? System.Windows.WindowState.Normal
            : System.Windows.WindowState.Maximized;
    }
}
