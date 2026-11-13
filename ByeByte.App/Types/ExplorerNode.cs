using System.Collections.ObjectModel;
using System.ComponentModel;

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
