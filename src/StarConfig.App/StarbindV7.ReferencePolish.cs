using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace StarConfig;

public sealed partial class StarbindV5Window
{
    private void ApplyReferencePolish()
    {
        Dispatcher.BeginInvoke(() =>
        {
            ApplyReferenceSizing();
            ApplyToolRowSpacing();
            SelectMostAssignedControl();
            WidenJoystickArtwork();
        }, DispatcherPriority.Loaded);
    }

    private void ApplyReferenceSizing()
    {
        if (Content is not Grid outer || outer.ColumnDefinitions.Count < 2) return;
        outer.ColumnDefinitions[0].Width = new GridLength(280);
        var application = outer.Children.OfType<Grid>().FirstOrDefault(element => Grid.GetColumn(element) == 1);
        if (application is null) return;

        var main = application.Children.OfType<Grid>().FirstOrDefault(element => Grid.GetRow(element) == 2);
        if (main is not null && main.ColumnDefinitions.Count >= 4)
        {
            main.ColumnDefinitions[0].Width = new GridLength(190);
            main.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
            main.ColumnDefinitions[2].Width = new GridLength(195);
            main.ColumnDefinitions[3].Width = new GridLength(310);
        }

        var bottom = application.Children.OfType<Grid>().FirstOrDefault(element => Grid.GetRow(element) == 3);
        if (bottom is not null && bottom.ColumnDefinitions.Count >= 3)
        {
            bottom.ColumnDefinitions[0].Width = new GridLength(255);
            bottom.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
            bottom.ColumnDefinitions[2].Width = new GridLength(465);
        }
    }

    private void ApplyToolRowSpacing()
    {
        _view3DButton.FontSize = 9;
        _view3DButton.Padding = new Thickness(8, 6, 8, 6);
        _testButton.FontSize = 9;
        _testButton.Padding = new Thickness(8, 6, 8, 6);
        _showUnassigned.Content = "Show Unassigned Only";
        _showUnassigned.FontSize = 8.5;
        _showUnassigned.Margin = new Thickness(7, 0, 14, 0);
        _zoomPicker.Width = 70;

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
        foreach (var image in Descendants<Image>(_deviceCanvasHost))
        {
            if (image.Width < 300 || image.Height < 300 || image.Stretch == Stretch.Fill) continue;
            image.Stretch = Stretch.Fill;
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
        }
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match) yield return match;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
}
