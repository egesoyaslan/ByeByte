using ByteFinder.Core.Scanner;
using ByteFinder.Core.Types;
using ByteFinder.App.Types;
using System.Windows.Controls;
using System.IO;
using System.Windows.Media;
using System.Drawing;
using DrawingIcon = System.Drawing.Icon;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace ByteFinder.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private readonly Win32Scanner _scanner = new();
    private IReadOnlyList<DriveDetails> _drives = [];
    private DriveDetails? _selectedDrive;
    private string? _selectedScanPath;
    private bool _isFolderScan;
    private ScanResult? _scanResult;

    public MainWindow()
    {
        InitializeComponent();

        PopulateDriveMenu(_scanner.GetReadyDrives());
    }

    private void Window_OnPreviewMouseLeftButtonDown(
        object sender,
        System.Windows.Input.MouseButtonEventArgs e)
    {
        if (!SelectedEntryFullPath.IsMouseOver &&
            (CopyFullPathPopup.IsOpen || SelectedEntryFullPath.IsFocused))
        {
            ClearFullPathInteraction();
        }
    }

    private void ClearFullPathInteraction()
    {
        CopyFullPathPopup.IsOpen = false;
        SelectedEntryFullPath.IsHitTestVisible = true;
        SelectedEntryFullPath.Select(0, 0);
        System.Windows.Input.FocusManager.SetFocusedElement(this, null);
        System.Windows.Input.Keyboard.ClearFocus();
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
        _drives = drives;
        string? systemDrivePath = Path.GetPathRoot(Environment.SystemDirectory);

        foreach (var drive in drives)
        {
            var item = new MenuItem // MenuItem is a XAML element that is the item in a dropdown menu. It comes with System.Windows.Controls.
            {
                Header = GetDriveDisplayName(drive),
                Tag = drive
            };

            item.Click += DriveMenuItem_OnClick;
            DriveMenu.Items.Add(item);
            
            // This checks if the drive added to the dropdown menu is the system drive. If it is the system drive is selected as default.
            if (string.Equals(drive.RootPath, systemDrivePath,
                    StringComparison.OrdinalIgnoreCase))
            {
                SelectDrive(drive);
            }
        }
        
        // This is just a fallback so even if a system drive isn't found the first scanned drive is selected as default.
        if (_selectedDrive is null && drives.Count > 0)
        {
            SelectDrive(drives[0]);
        }

        DriveMenu.Items.Add(new Separator());

        var selectFolderItem = new MenuItem
        {
            Header = "Select a Folder"
        };

        selectFolderItem.Click += SelectFolderMenuItem_OnClick; // += operator is used to add a handler to an event.
        DriveMenu.Items.Add(selectFolderItem);
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
        _selectedScanPath = drive.RootPath;
        _isFolderScan = false;
        DriveDropDownButton.Content = GetDriveDisplayName(drive);

        UpdateDriveSummary(drive);
    }

    private void SelectFolderMenuItem_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        // This opens the windows file explorer and prompts the user to select a folder to scan.
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select a Folder to Scan"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        string? rootPath = Path.GetPathRoot(dialog.FolderName);
        
        // This checks the root of the selected folder to find what drive it belongs to.
        DriveDetails? drive = _drives
            .Where(item => string.Equals(item.RootPath, rootPath, StringComparison.OrdinalIgnoreCase))
            .Select(item => (DriveDetails?)item)
            .FirstOrDefault();

        // NOTE: BETTER EXCEPTION HANDLING MAY BE NEEDED HERE.
        // If the selected folders root doesn't match with a recognized drive, we return.
        if (drive is not { } selectedDrive)
        {
            return;
        }

        _selectedDrive = selectedDrive;
        _selectedScanPath = dialog.FolderName;
        _isFolderScan = true;
        DriveDropDownButton.Content = $"Folder: {dialog.FolderName}";
        
        // The drive summary UI is updated to the selected folders root drive.
        UpdateDriveSummary(selectedDrive);
    }

    private void UpdateDriveSummary(DriveDetails drive)
    {
        long usedSpace = drive.TotalSize - drive.AvailableFreeSpace;

        SelectedDriveTextBlock.Text = drive.RootPath;
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

    private void SetScanPercentage(double percentage)
    {
        ScanProgressBar.Width = Math.Clamp(percentage, 0, 100) * 2.8;
    }

    private async void ScanButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_selectedDrive is not { } drive || _selectedScanPath is not { } scanPath)
        {
            return;
        }

        long usedSpace = drive.TotalSize - drive.AvailableFreeSpace;
        var progress = new Progress<ScanProgress>(scanProgress =>
        {
            if (!_isFolderScan)
            {
                double percentage = usedSpace == 0
                    ? 0
                    : scanProgress.ScannedBytes * 100d / usedSpace;

                SetScanPercentage(percentage);
            }

            ExplorerPlaceholder.Text = $"Scanning Files: {scanProgress.ScannedFileCount:N0} scanned.";
        });

        DriveDropDownButton.IsEnabled = false;
        ScanButton.IsEnabled = false;
        SetScanPercentage(0);
        ExplorerPlaceholder.Text = "Scanning Files: 0 scanned.";
        ExplorerPlaceholder.Visibility = System.Windows.Visibility.Visible;
        ExplorerResultsTree.Visibility = System.Windows.Visibility.Collapsed;

        try
        {
            // Since we have to pass a method to Task.Run for it to work properly, we use a lambda function to avoid defining a method just for this.
            ScanResult result = await Task.Run(() => _scanner.ScanDirectory(scanPath, progress));
            _scanResult = result;
            ShowScanResults(result, usedSpace);
            SetScanPercentage(100);
        }
        finally
        {
            DriveDropDownButton.IsEnabled = true;
            ScanButton.IsEnabled = true;
        }
    }

    private void ShowScanResults(ScanResult result, long usedSpace)
    {
        var entriesByParent = result.Entries
            .Select((entry, index) => new IndexedEntry(index, entry))
            .Where(indexedEntry => !indexedEntry.Entry.IsHidden)
            .ToLookup(indexedEntry => indexedEntry.Entry.ParentIndex); // This groups files by ParentIndex which is defined in Win32Scanner.cs.
        
        // The ParentIndex 0 represents the files and folders that are directly in the root directory.
        var roots = entriesByParent[0]
            .OrderByDescending(indexedEntry => indexedEntry.Entry.Size)
            .Select(indexedEntry => CreateExplorerNode(
                indexedEntry,
                entriesByParent,
                result,
                usedSpace,
                depth: 0))
            .ToList();

        ExplorerResultsTree.ItemsSource = roots;
        ExplorerPlaceholder.Visibility = System.Windows.Visibility.Collapsed;
        ExplorerResultsTree.Visibility = System.Windows.Visibility.Visible;
    }

    private void ExplorerResultsTree_OnSelectedItemChanged(
        object sender,
        System.Windows.RoutedPropertyChangedEventArgs<object> e)
    {
        if (_scanResult is null || e.NewValue is not ExplorerNode node)
        {
            return;
        }

        string fullPath = _scanResult.GetFullPath(node.FileIndex);

        CopyFullPathPopup.IsOpen = false;
        SelectedEntryFullPath.IsHitTestVisible = true;
        SelectedEntryFullPath.Text = fullPath;
        SelectedEntryFullPath.Select(0, 0);
        SelectedEntryFullPath.ScrollToHome();
        SelectedEntryIcon.Source = node.IsDirectory
            ? GetPngIcon(Path.Combine(AppContext.BaseDirectory, "assets", "png", "yellow.png"))
            : GetEntryIcon(fullPath);
    }

    private void SelectedEntryFullPath_OnPreviewMouseLeftButtonUp(
        object sender,
        System.Windows.Input.MouseButtonEventArgs e)
    {
        SelectedEntryFullPath.Focus();
        SelectedEntryFullPath.SelectAll();
        CopyFullPathPopup.IsOpen = true;
        SelectedEntryFullPath.IsHitTestVisible = false;
    }

    private void CopyFullPathButton_OnClick(
        object sender,
        System.Windows.RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(SelectedEntryFullPath.Text))
        {
            System.Windows.Clipboard.SetText(SelectedEntryFullPath.Text);
            ClearFullPathInteraction();
        }
    }

    private ExplorerNode CreateExplorerNode(
        IndexedEntry indexedEntry,
        ILookup<int, IndexedEntry> entriesByParent,
        ScanResult result,
        long usedSpace,
        int depth)
    {
        var children = entriesByParent[indexedEntry.Index]
            .OrderByDescending(child => child.Entry.Size)
            
            // Child entries are made until the max depth is reached using recursion.
            .Select(child => CreateExplorerNode(
                child,
                entriesByParent,
                result,
                usedSpace,
                depth + 1))
            .ToList();

        FileEntry entry = indexedEntry.Entry;

        return new ExplorerNode(
            entry.Name,
            FormatBytes(entry.Size),
            $"{GetPercentage(entry.Size, GetPercentageDenominator(result, entry, usedSpace))}%",
            GetEntryExtension(entry),
            entry.IsDirectory,
            indexedEntry.Index,
            new System.Windows.Thickness(depth * 16, 0, 0, 0), // This is the indent I had to manually do since TreeView indents all columns by default.
            children);
    }

    private long GetPercentageDenominator(ScanResult result, FileEntry entry, long usedSpace)
    {
        return entry.ParentIndex == 0 && !_isFolderScan
            ? usedSpace
            : result.Entries[entry.ParentIndex].Size;
    }

    private static string GetEntryExtension(FileEntry entry)
    {
        if (entry.IsDirectory)
        {
            return "Folder";
        }

        string extension = Path.GetExtension(entry.Name);
        return string.IsNullOrEmpty(extension) ? "-" : extension;
    }

    private static ImageSource? GetEntryIcon(string fullPath)
    {
        try // File may be deleted by the time we try to fetch icon.
        {
            using Icon? icon = DrawingIcon.ExtractAssociatedIcon(fullPath); // using directive so we dispose of the image after.
            
            if (icon is null)
                return null;
        
            var image = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(32, 32));

            image.Freeze();
            return image;
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }
    
    private static ImageSource? GetPngIcon(string pngPath)
    {
        try
        {
            var image = new BitmapImage();

            image.BeginInit();
            image.UriSource = new Uri(pngPath, UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();

            image.Freeze();
            return image;
        }
        catch (IOException)
        {
            return null;
        }
    }
}
