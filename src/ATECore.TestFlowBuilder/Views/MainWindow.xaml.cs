using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using ATECore.TestFlowBuilder.Models;
using ATECore.TestFlowBuilder.ViewModels;

namespace ATECore.TestFlowBuilder.Views;

public partial class MainWindow : Window
{
    private Point _dragOrigin;
    private string? _dragKey;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Hook();
    }

    private MainViewModel? Vm => DataContext as MainViewModel;

    private void Hook()
    {
        if (Vm is not { } vm) return;
        vm.Chat.CollectionChanged += (_, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
                Dispatcher.BeginInvoke(() => ChatScroll.ScrollToEnd(), System.Windows.Threading.DispatcherPriority.Background);
        };
        vm.CellAdded += _ =>
            Dispatcher.BeginInvoke(() => FlowScroll.ScrollToEnd(), System.Windows.Threading.DispatcherPriority.Background);
    }

    // ── Library: drag out, double-click to add ─────────────────────────────

    private void LibraryItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragKey = null;
        if (e.OriginalSource is DependencyObject source && FindAncestor<ButtonBase>(source) is not null) return;
        if (sender is not FrameworkElement { Tag: string key }) return;

        if (e.ClickCount == 2)
        {
            Vm?.AddTest(key);
            e.Handled = true;
            return;
        }

        _dragKey = key;
        _dragOrigin = e.GetPosition(this);
    }

    private void LibraryItem_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragKey is null || e.LeftButton != MouseButtonState.Pressed || sender is not DependencyObject item) return;

        var delta = e.GetPosition(this) - _dragOrigin;
        if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance) return;

        var key = _dragKey;
        _dragKey = null;
        try { DragDrop.DoDragDrop(item, new DataObject(DataFormats.StringFormat, key), DragDropEffects.Copy); }
        finally { if (Vm is { } vm) vm.DropActive = false; }
    }

    // ── Flow: drop target ──────────────────────────────────────────────────

    private static bool IsLibraryDrag(DragEventArgs e, out string key)
    {
        key = e.Data.GetDataPresent(DataFormats.StringFormat) ? (e.Data.GetData(DataFormats.StringFormat) as string ?? "") : "";
        return TestLibrary.Find(key) is not null;
    }

    private void DropZone_DragOver(object sender, DragEventArgs e)
    {
        var ok = IsLibraryDrag(e, out _);
        e.Effects = ok ? DragDropEffects.Copy : DragDropEffects.None;
        if (Vm is { } vm) vm.DropActive = ok;
        e.Handled = true;
    }

    private void DropZone_DragLeave(object sender, DragEventArgs e)
    {
        if (Vm is { } vm) vm.DropActive = false;
    }

    private void DropZone_Drop(object sender, DragEventArgs e)
    {
        if (IsLibraryDrag(e, out var key)) Vm?.AddTest(key);
        else if (Vm is { } vm) vm.DropActive = false;
        e.Handled = true;
    }

    private static T? FindAncestor<T>(DependencyObject start) where T : DependencyObject
    {
        for (var node = start; node is not null; node = node is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(node) : LogicalTreeHelper.GetParent(node))
            if (node is T match) return match;
        return null;
    }
}
