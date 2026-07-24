using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace StarConfig;

public sealed class StateConfirmationWindow : Window
{
    private readonly IReadOnlyList<StateAssignment> _assignments;
    private readonly Dictionary<string, CheckBox> _checks = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<StateConfirmationResult> Results { get; private set; } = [];

    public StateConfirmationWindow(StarbindControl control, IReadOnlyList<StateAssignment> assignments)
    {
        _assignments = assignments;
        Title = "Confirm State Bindings";
        Width = 570;
        Height = 560;
        MinWidth = 500;
        MinHeight = 440;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = StarbindWindow.Bg;
        Foreground = StarbindWindow.Text;
        FontFamily = new FontFamily("Segoe UI");
        Content = Build(control);
    }

    private UIElement Build(StarbindControl control)
    {
        var root = new Grid { Margin = new Thickness(16) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var intro = new StackPanel();
        intro.Children.Add(new TextBlock { Text = "DUPLICATE / SIMILAR ACTION CHECK", Foreground = StarbindWindow.Cyan, FontWeight = FontWeights.Bold, FontSize = 13 });
        intro.Children.Add(new TextBlock { Text = $"{control.DisplayName} ({control.Input}) can be used in several game states. Keep the states that should share this physical control and clear the rest.", Foreground = StarbindWindow.Muted, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 12) });
        root.Children.Add(intro);

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var rows = new StackPanel();
        foreach (var assignment in _assignments)
        {
            var card = new Border { Background = StarbindWindow.Panel, BorderBrush = assignment.HasConflict ? StarbindWindow.Amber : StarbindWindow.Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 7) };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            var check = new CheckBox { Content = assignment.Context, IsChecked = assignment.Enabled, Foreground = StarbindWindow.Text, VerticalAlignment = VerticalAlignment.Center };
            _checks[assignment.Context] = check;
            grid.Children.Add(check);
            var details = new StackPanel();
            details.Children.Add(new TextBlock { Text = assignment.SelectedAction?.DisplayName ?? "No action selected", Foreground = StarbindWindow.Text, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
            details.Children.Add(new TextBlock { Text = assignment.SelectedAction is null ? "This state will remain unassigned." : $"{assignment.SelectedAction.Behavior} • {assignment.SelectedAction.Intent}", Foreground = assignment.HasConflict ? StarbindWindow.Amber : StarbindWindow.Muted, FontSize = 10, TextWrapping = TextWrapping.Wrap });
            Grid.SetColumn(details, 1);
            grid.Children.Add(details);
            card.Child = grid;
            rows.Children.Add(card);
        }
        scroll.Content = rows;
        Grid.SetRow(scroll, 1);
        root.Children.Add(scroll);

        var buttons = new Grid { Margin = new Thickness(0, 12, 0, 0) };
        buttons.ColumnDefinitions.Add(new ColumnDefinition());
        buttons.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        buttons.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var keepAll = MakeButton("KEEP ALL CURRENT STATES", StarbindWindow.Panel2);
        keepAll.Click += (_, _) => { foreach (var check in _checks.Values) check.IsChecked = true; };
        buttons.Children.Add(keepAll);
        var cancel = MakeButton("CANCEL", StarbindWindow.Panel2);
        cancel.Click += (_, _) => { DialogResult = false; Close(); };
        Grid.SetColumn(cancel, 1);
        buttons.Children.Add(cancel);
        var confirm = MakeButton("CONFIRM", StarbindWindow.BlueDim);
        confirm.Click += (_, _) =>
        {
            Results = _assignments.Select(a => new StateConfirmationResult(a.Context, _checks[a.Context].IsChecked == true, a.SelectedAction)).ToList();
            DialogResult = true;
            Close();
        };
        Grid.SetColumn(confirm, 2);
        buttons.Children.Add(confirm);
        Grid.SetRow(buttons, 2);
        root.Children.Add(buttons);
        return root;
    }

    private static Button MakeButton(string text, Brush brush) => new() { Content = text, Background = brush, Foreground = StarbindWindow.Text, BorderBrush = StarbindWindow.Border, Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(5, 0, 0, 0), Cursor = Cursors.Hand };
}

public sealed record StateConfirmationResult(string Context, bool Enabled, StarbindAction? Action);

public sealed class StarbindPreferenceStore
{
    private readonly string _folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Starbind");
    private string FilePath => Path.Combine(_folder, "settings.json");
    public string? LastProfile { get; set; }

    public StarbindPreferenceStore()
    {
        try
        {
            if (!File.Exists(FilePath)) return;
            var model = JsonSerializer.Deserialize<PreferenceModel>(File.ReadAllText(FilePath));
            LastProfile = model?.LastProfile;
        }
        catch { }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(_folder);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(new PreferenceModel(LastProfile), new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    private sealed record PreferenceModel(string? LastProfile);
}

public sealed partial class StarbindWindow
{
    private static Border Card() => new() { Background = Panel, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5) };
    private static TextBlock PanelTitle(string text) => new() { Text = text, FontSize = 11, FontWeight = FontWeights.Bold, Foreground = Text };
    private static TextBlock SmallLabel(string text, Thickness margin) => new() { Text = text, FontSize = 9, Foreground = Faint, Margin = margin };

    private static StackPanel HeaderStat(string label, TextBlock value)
    {
        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0) };
        stack.Children.Add(new TextBlock { Text = label, Foreground = Faint, FontSize = 8 });
        value.FontWeight = FontWeights.SemiBold;
        value.FontSize = 11;
        stack.Children.Add(value);
        return stack;
    }

    private static Button TopButton(string text, RoutedEventHandler click)
    {
        var button = new Button { Content = text, Background = Brushes.Transparent, Foreground = Muted, BorderBrush = Brushes.Transparent, Padding = new Thickness(9, 6, 9, 6), Cursor = Cursors.Hand };
        button.Click += click;
        return button;
    }

    private static Button FooterButton(string text, RoutedEventHandler click)
    {
        var button = new Button { Content = text, Background = Panel2, Foreground = Text, BorderBrush = Border, Padding = new Thickness(13, 9, 13, 9), Margin = new Thickness(4, 0, 0, 0), Cursor = Cursors.Hand };
        button.Click += click;
        return button;
    }

    private static void ConfigureButton(Button button, string text, RoutedEventHandler click, Brush background)
    {
        button.Content = text;
        button.Background = background;
        button.Foreground = Text;
        button.BorderBrush = Border2;
        button.Padding = new Thickness(10, 7, 10, 7);
        button.Margin = new Thickness(4, 0, 0, 0);
        button.Cursor = Cursors.Hand;
        button.Click += click;
    }

    private static void StyleCombo(ComboBox combo)
    {
        combo.Background = Field;
        combo.Foreground = Text;
        combo.BorderBrush = Border;
        combo.Padding = new Thickness(6, 4, 6, 4);
    }

    private static void StyleTextBox(TextBox box)
    {
        box.Background = Field;
        box.Foreground = Text;
        box.BorderBrush = Border;
        box.CaretBrush = Text;
        box.Padding = new Thickness(6, 5, 6, 5);
    }

    private static void StyleTree(TreeView tree)
    {
        tree.Background = Field;
        tree.Foreground = Text;
        tree.BorderBrush = Border;
        tree.Padding = new Thickness(3);
    }

    private static void StyleList(ListBox list)
    {
        list.Background = Field;
        list.Foreground = Text;
        list.BorderBrush = Border;
        list.Padding = new Thickness(3);
    }

    private static TreeViewItem TreeGroup(string text) => new() { Header = new TextBlock { Text = text, Foreground = Muted, FontWeight = FontWeights.SemiBold }, Foreground = Text, Padding = new Thickness(3) };

    private static DataTemplate WarningTemplate()
    {
        var template = new DataTemplate(typeof(StarbindWarning));
        var panel = new FrameworkElementFactory(typeof(DockPanel));
        panel.SetValue(FrameworkElement.MarginProperty, new Thickness(2, 4, 2, 4));
        var icon = new FrameworkElementFactory(typeof(TextBlock));
        icon.SetBinding(TextBlock.TextProperty, new Binding(nameof(StarbindWarning.Icon)));
        icon.SetBinding(TextBlock.ForegroundProperty, new Binding(nameof(StarbindWarning.Color)));
        icon.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
        icon.SetValue(FrameworkElement.WidthProperty, 20d);
        icon.SetValue(DockPanel.DockProperty, Dock.Left);
        panel.AppendChild(icon);
        var text = new FrameworkElementFactory(typeof(TextBlock));
        text.SetBinding(TextBlock.TextProperty, new Binding(nameof(StarbindWarning.Text)));
        text.SetValue(TextBlock.ForegroundProperty, Muted);
        text.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        panel.AppendChild(text);
        template.VisualTree = panel;
        return template;
    }
}
