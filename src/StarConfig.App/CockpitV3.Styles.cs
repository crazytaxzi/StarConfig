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

public sealed partial class CockpitWindow
{
    private static Border Card() => new() { Background = PanelBg, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6) };
    private static TextBlock PanelTitle(string text) => new() { Text = text, Foreground = Text, FontSize = 11, FontWeight = FontWeights.Bold };

    private static StackPanel HeaderStat(string label, TextBlock value)
    {
        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 12, 0) };
        stack.Children.Add(new TextBlock { Text = label, Foreground = Faint, FontSize = 9 });
        value.FontSize = 12;
        value.FontWeight = FontWeights.SemiBold;
        value.Margin = new Thickness(0, 3, 0, 0);
        stack.Children.Add(value);
        return stack;
    }

    private static StackPanel SmallStat(string label, TextBlock value)
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = label, Foreground = Faint, FontSize = 9 });
        value.Foreground = Text;
        value.Margin = new Thickness(0, 3, 0, 0);
        stack.Children.Add(value);
        return stack;
    }

    private static Button FlatButton(string text, RoutedEventHandler click)
    {
        var button = new Button { Content = text, Background = PanelAlt, Foreground = Text, BorderBrush = Border, Padding = new Thickness(11, 7, 11, 7), Margin = new Thickness(4, 0, 0, 0), Cursor = Cursors.Hand };
        button.Click += click;
        return button;
    }

    private static Button PrimaryButton(string text, RoutedEventHandler click)
    {
        var button = new Button { Content = text, Background = BlueDim, Foreground = Text, BorderBrush = Blue, Padding = new Thickness(14, 8, 14, 8), Cursor = Cursors.Hand, HorizontalAlignment = HorizontalAlignment.Left };
        button.Click += click;
        return button;
    }

    private static void ConfigureButton(Button button, string text, RoutedEventHandler click, Brush? background = null)
    {
        button.Content = text;
        button.Background = background ?? PanelAlt;
        button.Foreground = Text;
        button.BorderBrush = BorderStrong;
        button.Padding = new Thickness(11, 7, 11, 7);
        button.Margin = new Thickness(4, 0, 0, 0);
        button.Cursor = Cursors.Hand;
        button.Click += click;
    }

    private static void StyleCombo(ComboBox combo)
    {
        combo.Background = FieldBg;
        combo.Foreground = Text;
        combo.BorderBrush = Border;
        combo.Padding = new Thickness(7, 4, 7, 4);
    }

    private static void StyleTextBox(TextBox box)
    {
        box.Background = FieldBg;
        box.Foreground = Text;
        box.BorderBrush = Border;
        box.Padding = new Thickness(7, 5, 7, 5);
        box.CaretBrush = Text;
    }

    private static void StyleTree(TreeView tree)
    {
        tree.Background = FieldBg;
        tree.Foreground = Text;
        tree.BorderBrush = Border;
        tree.Padding = new Thickness(4);
    }

    private static void StyleList(ListBox list)
    {
        list.Background = FieldBg;
        list.Foreground = Text;
        list.BorderBrush = Border;
        list.Padding = new Thickness(3);
    }

    private static TreeViewItem TreeGroup(string text) => new() { Header = new TextBlock { Text = text, FontWeight = FontWeights.SemiBold, Foreground = Muted }, Foreground = Text, Padding = new Thickness(3) };
    private static TreeViewItem TreeControl(PhysicalControl control) => new() { Header = control.Name, Tag = control, Foreground = Text, Padding = new Thickness(3) };

    private static DataTemplate WarningTemplate()
    {
        var template = new DataTemplate(typeof(WarningItem));
        var panel = new FrameworkElementFactory(typeof(DockPanel));
        panel.SetValue(FrameworkElement.MarginProperty, new Thickness(2, 4, 2, 4));
        var icon = new FrameworkElementFactory(typeof(TextBlock));
        icon.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(WarningItem.Icon)));
        icon.SetBinding(TextBlock.ForegroundProperty, new System.Windows.Data.Binding(nameof(WarningItem.Color)));
        icon.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
        icon.SetValue(FrameworkElement.WidthProperty, 20d);
        icon.SetValue(DockPanel.DockProperty, Dock.Left);
        panel.AppendChild(icon);
        var text = new FrameworkElementFactory(typeof(TextBlock));
        text.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(WarningItem.Text)));
        text.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        text.SetValue(TextBlock.ForegroundProperty, Muted);
        panel.AppendChild(text);
        template.VisualTree = panel;
        return template;
    }

    private static Brush Paint(string hex) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
}
