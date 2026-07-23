using System.Windows;
using System.Windows.Controls;
using StarConfig.Models;

namespace StarConfig;

public sealed class IntentSuggestionWindow : Window
{
    private readonly List<(BindingEntry Binding, CheckBox Box)> _rows = [];
    public IReadOnlyList<BindingEntry> SelectedBindings => _rows.Where(x => x.Box.IsChecked == true).Select(x => x.Binding).ToList();

    public IntentSuggestionWindow(BindingEntry current, IEnumerable<BindingEntry> candidates, string input)
    {
        Title = "Apply control across game states";
        Width = 620;
        Height = 520;
        MinWidth = 520;
        MinHeight = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = System.Windows.Media.Brushes.Black;
        Foreground = System.Windows.Media.Brushes.White;

        var root = new DockPanel { Margin = new Thickness(18) };
        Content = root;

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        DockPanel.SetDock(buttons, Dock.Bottom);
        var cancel = new Button { Content = "Cancel", Margin = new Thickness(6), Padding = new Thickness(14, 7, 14, 7), IsCancel = true };
        var apply = new Button { Content = "Apply Selected", Margin = new Thickness(6), Padding = new Thickness(14, 7, 14, 7), IsDefault = true };
        apply.Click += (_, _) => { DialogResult = true; Close(); };
        buttons.Children.Add(cancel);
        buttons.Children.Add(apply);
        root.Children.Add(buttons);

        var intro = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        DockPanel.SetDock(intro, Dock.Top);
        intro.Children.Add(new TextBlock { Text = "This control appears to represent the same player intent in several game states.", FontSize = 17, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
        intro.Children.Add(new TextBlock { Text = $"Input: {input}\nChoose exactly where it should be applied. Cycle, Toggle, and Direct Select actions are never merged by this assistant.", Margin = new Thickness(0, 8, 0, 0), Opacity = 0.78, TextWrapping = TextWrapping.Wrap });
        root.Children.Add(intro);

        var panel = new StackPanel();
        foreach (var binding in new[] { current }.Concat(candidates).DistinctBy(x => x.Identity))
        {
            var box = new CheckBox { IsChecked = binding.Identity == current.Identity, Content = $"{binding.Context}: {binding.ActionName}", Margin = new Thickness(4, 8, 4, 8), FontSize = 15 };
            _rows.Add((binding, box));
            panel.Children.Add(box);
            panel.Children.Add(new TextBlock { Text = binding.Description, Margin = new Thickness(28, -3, 4, 6), Opacity = 0.68, TextWrapping = TextWrapping.Wrap });
        }
        root.Children.Add(new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
    }
}
