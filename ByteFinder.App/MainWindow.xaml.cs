using System.Diagnostics;
using ByteFinder.Core.Scanner;
using ByteFinder.Core.Types;
using System.Windows.Controls;
using System.IO;

namespace ByteFinder.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private DriveDetails? _selectedDrive;

    public MainWindow()
    {
        InitializeComponent();

        var scanner = new Win32Scanner();

        PopulateDriveMenu(scanner.GetReadyDrives());

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

    private void PopulateDriveMenu(IReadOnlyList<DriveDetails> drives)
    {
        string? systemDrivePath = Path.GetPathRoot(Environment.SystemDirectory);

        foreach (var drive in drives)
        {
            var item = new MenuItem
            {
                Header = GetDriveDisplayName(drive),
                Tag = drive
            };

            item.Click += DriveMenuItem_OnClick;
            DriveMenu.Items.Add(item);

            if (string.Equals(drive.RootPath, systemDrivePath,
                    StringComparison.OrdinalIgnoreCase))
            {
                SelectDrive(drive);
            }
        }

        if (_selectedDrive is null && drives.Count > 0)
        {
            SelectDrive(drives[0]);
        }
    }

    private void DriveMenuItem_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: DriveDetails drive })
        {
            SelectDrive(drive);
        }
    }

    private void SelectDrive(DriveDetails drive)
    {
        _selectedDrive = drive;
        DriveDropDownButton.Content = GetDriveDisplayName(drive);

        long usedSpace = drive.TotalSize - drive.AvailableFreeSpace;

        SelectedDriveTextBlock.Text = $"{drive.RootPath}";
        TotalSpaceTextBlock.Text = $"{FormatBytes(drive.TotalSize)}";
        UsedSpaceTextBlock.Text = $"{FormatBytes(usedSpace)} (%{GetPercentage(usedSpace, drive.TotalSize)})";
        FreeSpaceTextBlock.Text = $"{FormatBytes(drive.AvailableFreeSpace)} (%{GetPercentage(drive.AvailableFreeSpace, drive.TotalSize)})";
    }

    private static string GetDriveDisplayName(DriveDetails drive)
    {
        return string.Equals(drive.Name, drive.RootPath, StringComparison.OrdinalIgnoreCase)
            ? drive.RootPath
            : $"{drive.Name} ({drive.RootPath})";
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        int unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.#} {units[unitIndex]}";
    }

    private static int GetPercentage(long value, long total)
    {
        return total == 0
            ? 0
            : (int)Math.Round((double)value / total * 100);
    }
}
