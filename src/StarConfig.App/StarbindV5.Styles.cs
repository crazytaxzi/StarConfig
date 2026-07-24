using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace StarConfig;

public sealed partial class StarbindV5Window
{
    private static Border Card() => new()
    {
        Background = Panel,
        BorderBrush = Border,
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(5)
    };

    private static TextBlock PanelTitle(string text) => new()
    {
        Text = text,
        FontSize = 11,
        FontWeight = FontWeights.Bold,
        Foreground = Text
    };

    private static TextBlock SmallLabel(string text, Thickness margin) => new()
    {
        Text = text,
        FontSize = 9,
        Foreground = Faint,
        Margin = margin
    };

    private static StackPanel HeaderStat(string label, TextBlock value)
    {
        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 8, 0) };
        stack.Children.Add(new TextBlock { Text = label, Foreground = Faint, FontSize = 8 });
        value.FontWeight = FontWeights.SemiBold;
        value.FontSize = 11;
        stack.Children.Add(value);
        return stack;
    }

    private static Button TopButton(string text, RoutedEventHandler click)
    {
        var button = new Button
        {
            Content = text,
            Background = Brushes.Transparent,
            Foreground = Muted,
            BorderBrush = Brushes.Transparent,
            Padding = new Thickness(9, 6, 9, 6),
            Cursor = Cursors.Hand
        };
        button.Click += click;
        return button;
    }

    private static Button FooterButton(string text, RoutedEventHandler click)
    {
        var button = new Button
        {
            Content = text,
            Background = Panel2,
            Foreground = Text,
            BorderBrush = Border,
            Padding = new Thickness(13, 9, 13, 9),
            Margin = new Thickness(4, 0, 0, 0),
            Cursor = Cursors.Hand
        };
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

    private static TreeViewItem TreeGroup(string text) => new()
    {
        Header = new TextBlock { Text = text, Foreground = Muted, FontWeight = FontWeights.SemiBold },
        Foreground = Text,
        Padding = new Thickness(3)
    };

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

    internal static Button DialogButton(string text, Brush background)
    {
        return new Button
        {
            Content = text,
            Background = background,
            Foreground = Text,
            BorderBrush = Border2,
            Padding = new Thickness(12, 8, 12, 8),
            Margin = new Thickness(5, 0, 0, 0),
            Cursor = Cursors.Hand
        };
    }
}
