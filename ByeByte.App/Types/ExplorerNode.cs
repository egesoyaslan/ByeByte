using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;

namespace ByeByte.App.Types;

public sealed record ExplorerNode(
    string Name,
    string Size,
    string OccupiedPercentage,
    string Extension,
    bool IsDirectory,
    int FileIndex,
    System.Windows.Thickness NameIndent,
    ObservableCollection<ExplorerNode> Children) : INotifyPropertyChanged
{
    private bool _isExpanded;
    private bool _isSelected;

    public bool HasChildren => Children.Count > 0;
    public long SizeBytes { get; init; }

    public Brush OccupiedPercentageForeground
    {
        get
        {
            if (!int.TryParse(OccupiedPercentage.TrimEnd('%'), out int percentage))
                return Brushes.WhiteSmoke;

            percentage = Math.Clamp(percentage, 0, 100);
            Color green = Color.FromRgb(0x92, 0xF2, 0xA6);
            Color red = Color.FromRgb(0xFF, 0x91, 0x91);
            double amount = percentage / 100d;

            var brush = new SolidColorBrush(Color.FromRgb(
                (byte)Math.Round(green.R + (red.R - green.R) * amount),
                (byte)Math.Round(green.G + (red.G - green.G) * amount),
                (byte)Math.Round(green.B + (red.B - green.B) * amount)));
            brush.Freeze();
            return brush;
        }
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
