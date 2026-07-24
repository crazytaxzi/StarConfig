using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace StarConfig;

public sealed partial class StarbindV5Window
{
    private void ApplyReferencePolish()
    {
        Dispatcher.BeginInvoke(() =>
        {
            InstallV8Corrections();
            InstallDeviceNavigationFix();
            ApplyReferenceSizing();
            ApplyToolRowSpacing();
            SelectMostAssignedControl();
            BuildCorrectedKeyboardControlTree();
            RefreshCorrectedDeviceArtwork();
            RefreshCorrectedDeviceThumbnails();
            BringSelectedDeviceCardIntoView();
        }, DispatcherPriority.Loaded);
    }

    private void ApplyReferenceSizing()
    {
        if (Content is not Grid outer || outer.ColumnDefinitions.Count < 2) return;
        outer.ColumnDefinitions[0].Width = new GridLength(0);
        foreach (var element in outer.Children.OfType<UIElement>().Where(element => Grid.GetColumn(element) == 0))
            element.Visibility = Visibility.Collapsed;

        var application = outer.Children.OfType<Grid>().FirstOrDefault(element => Grid.GetColumn(element) == 1);
        if (application is null) return;

        var main = application.Children.OfType<Grid>().FirstOrDefault(element => Grid.GetRow(element) == 2);
        if (main is not null && main.ColumnDefinitions.Count >= 4)
        {
            main.ColumnDefinitions[0].Width = new GridLength(230);
            main.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
            main.ColumnDefinitions[2].Width = new GridLength(230);
            main.ColumnDefinitions[3].Width = new GridLength(380);
        }

        var bottom = application.Children.OfType<Grid>().FirstOrDefault(element => Grid.GetRow(element) == 3);
        if (bottom is not null && bottom.ColumnDefinitions.Count >= 3)
        {
            bottom.ColumnDefinitions[0].Width = new GridLength(300);
            bottom.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
            bottom.ColumnDefinitions[2].Width = new GridLength(540);
        }
    }

    private void ApplyToolRowSpacing()
    {
        _view3DButton.Visibility = Visibility.Collapsed;
        _testButton.FontSize = 9;
        _testButton.Padding = new Thickness(10, 6, 10, 6);
        _showUnassigned.Content = "Show Unassigned Only";
        _showUnassigned.FontSize = 8.5;
        _showUnassigned.Margin = new Thickness(7, 0, 14, 0);
        _zoomPicker.Width = 72;

        ScrollViewer.SetHorizontalScrollBarVisibility(_controlTree, ScrollBarVisibility.Disabled);
        ScrollViewer.SetVerticalScrollBarVisibility(_controlTree, ScrollBarVisibility.Auto);
        ScrollViewer.SetHorizontalScrollBarVisibility(_actionTree, ScrollBarVisibility.Disabled);
        ScrollViewer.SetVerticalScrollBarVisibility(_actionTree, ScrollBarVisibility.Auto);
        ScrollViewer.SetHorizontalScrollBarVisibility(_warnings, ScrollBarVisibility.Disabled);
        ScrollViewer.SetVerticalScrollBarVisibility(_warnings, ScrollBarVisibility.Auto);
    }

    private void SelectMostAssignedControl()
    {
        if (_selectedDevice is null || _profile is null) return;
        var best = _hardware.BuildControls(_selectedDevice, _profile, _settings)
            .Select(control => new
            {
                Control = control,
                Assignments = CurrentAssignments(control).ToList()
            })
            .Where(candidate => candidate.Assignments.Count > 0)
            .OrderByDescending(candidate => candidate.Assignments.Select(action => action.Context).Distinct(StringComparer.OrdinalIgnoreCase).Count())
            .ThenByDescending(candidate => candidate.Assignments.Count)
            .ThenByDescending(candidate => candidate.Control.IsAxis)
            .ThenBy(candidate => candidate.Control.DisplayName)
            .FirstOrDefault();
        if (best is null || best.Control.Input.Equals(_selectedControl?.Input, StringComparison.OrdinalIgnoreCase)) return;
        SelectControl(best.Control);
    }

    private void WidenJoystickArtwork()
    {
        BuildCorrectedKeyboardControlTree();
        RefreshCorrectedDeviceArtwork();
    }
}
