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
                $"{i} | Parent={entry.ParentIndex} | " +
                $"{entry.Name} | {entry.Size} | " +
                $"Directory={entry.IsDirectory}"
            );
        }
    }
}