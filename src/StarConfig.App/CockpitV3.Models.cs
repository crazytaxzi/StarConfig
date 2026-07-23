using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ShapeEllipse = System.Windows.Shapes.Ellipse;
using ShapeLine = System.Windows.Shapes.Line;
using ShapeRectangle = System.Windows.Shapes.Rectangle;

namespace StarConfig;

public sealed record PhysicalControl(string Input, string Name, string Type, string Category);
public sealed record DeviceCardInfo(string Title, string Name, string Detail, string Icon, InputDevice? Device);
public sealed record WarningItem(string Icon, string Text, Brush Color);
public sealed record ActionOption(BindingEntry Binding, string Label);

public sealed class StateBindingRow
{
    public string Context { get; }
    public Border Root { get; }
    public CheckBox Include { get; } = new();
    public ComboBox ActionPicker { get; } = new();
    public TextBlock Status { get; } = new();
    public IReadOnlyList<BindingEntry> InitialBindings { get; }
    public bool HasExistingAssignments => InitialBindings.Count > 0;

    public StateBindingRow(string context, IReadOnlyList<BindingEntry> choices, IReadOnlyList<BindingEntry> initialBindings, Func<string, string> humanize, Brush text, Brush muted, Brush field, Brush border, Brush green, Brush amber)
    {
        Context = context;
        InitialBindings = initialBindings;
        Root = new Border { Background = field, BorderBrush = border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Padding = new Thickness(8), Margin = new Thickness(0, 0, 0, 6) };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(92) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(75) });
        Root.Child = grid;

        Include.Content = context;
        Include.Foreground = text;
        Include.VerticalAlignment = VerticalAlignment.Center;
        Include.IsChecked = initialBindings.Count > 0;
        grid.Children.Add(Include);

        var options = choices.Select(choice => new ActionOption(choice, humanize(choice.ActionName))).ToList();
        ActionPicker.ItemsSource = options;
        ActionPicker.DisplayMemberPath = nameof(ActionOption.Label);
        ActionPicker.Background = field;
        ActionPicker.Foreground = text;
        ActionPicker.BorderBrush = border;
        ActionPicker.Padding = new Thickness(5, 3, 5, 3);
        ActionPicker.ToolTip = "Choose the action for this game state";
        if (initialBindings.Count > 0) ActionPicker.SelectedItem = options.FirstOrDefault(option => option.Binding.Identity == initialBindings[0].Identity);
        Grid.SetColumn(ActionPicker, 1);
        grid.Children.Add(ActionPicker);

        Status.HorizontalAlignment = HorizontalAlignment.Right;
        Status.VerticalAlignment = VerticalAlignment.Center;
        Status.FontSize = 9;
        Status.FontWeight = FontWeights.Bold;
        Grid.SetColumn(Status, 2);
        grid.Children.Add(Status);
        if (initialBindings.Count > 1) SetStatus($"{initialBindings.Count} CONFLICTS", amber);
        else if (initialBindings.Count == 1) SetStatus("BOUND", green);
        else SetStatus("EMPTY", muted);
    }

    public void SetSelectedAction(BindingEntry action)
    {
        if (ActionPicker.ItemsSource is IEnumerable<ActionOption> options)
            ActionPicker.SelectedItem = options.FirstOrDefault(option => option.Binding.Identity == action.Identity);
    }

    public BindingEntry? SelectedBinding => (ActionPicker.SelectedItem as ActionOption)?.Binding;

    public void SetStatus(string text, Brush brush)
    {
        Status.Text = text;
        Status.Foreground = brush;
    }
}

public sealed class UserSettingsStore
{
    private readonly string _folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StarConfig");
    private string SettingsFile => System.IO.Path.Combine(_folder, "profile-folders.txt");
    private string LastProfileFile => System.IO.Path.Combine(_folder, "last-profile.txt");

    public IEnumerable<string> LoadFolders()
    {
        try { return File.Exists(SettingsFile) ? File.ReadAllLines(SettingsFile).Where(line => !string.IsNullOrWhiteSpace(line)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray() : []; }
        catch { return []; }
    }

    public string? LoadLastProfile()
    {
        try { return File.Exists(LastProfileFile) ? File.ReadAllText(LastProfileFile).Trim() : null; }
        catch { return null; }
    }

    public void Save(IEnumerable<string> folders, string? lastProfile)
    {
        try
        {
            Directory.CreateDirectory(_folder);
            File.WriteAllLines(SettingsFile, folders.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(lastProfile)) File.WriteAllText(LastProfileFile, lastProfile);
        }
        catch { }
    }
}
