using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace StarConfig;

public sealed partial class StarbindV5Window
{
    private void InstallControlStyles()
    {
        const string xaml = """
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Style x:Key="StarbindComboItem" TargetType="ComboBoxItem">
    <Setter Property="Foreground" Value="#F4F7FB"/>
    <Setter Property="Background" Value="#0D131A"/>
    <Setter Property="Padding" Value="8,6"/>
    <Setter Property="HorizontalContentAlignment" Value="Stretch"/>
    <Setter Property="Template">
      <Setter.Value>
        <ControlTemplate TargetType="ComboBoxItem">
          <Border x:Name="ItemBorder" Background="{TemplateBinding Background}" Padding="{TemplateBinding Padding}">
            <ContentPresenter/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property="IsHighlighted" Value="True"><Setter TargetName="ItemBorder" Property="Background" Value="#173A67"/></Trigger>
            <Trigger Property="IsSelected" Value="True"><Setter TargetName="ItemBorder" Property="Background" Value="#23528D"/></Trigger>
            <Trigger Property="IsEnabled" Value="False"><Setter Property="Opacity" Value="0.45"/></Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>
  <Style x:Key="StarbindCombo" TargetType="ComboBox">
    <Setter Property="Foreground" Value="#F4F7FB"/>
    <Setter Property="Background" Value="#0D131A"/>
    <Setter Property="BorderBrush" Value="#46576B"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="Padding" Value="8,5"/>
    <Setter Property="ItemContainerStyle" Value="{StaticResource StarbindComboItem}"/>
    <Setter Property="Template">
      <Setter.Value>
        <ControlTemplate TargetType="ComboBox">
          <Grid>
            <Border x:Name="Chrome" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" CornerRadius="3">
              <Grid>
                <ContentPresenter Margin="9,3,30,3" VerticalAlignment="Center" HorizontalAlignment="Left"
                                  Content="{TemplateBinding SelectionBoxItem}"
                                  ContentTemplate="{TemplateBinding SelectionBoxItemTemplate}"
                                  ContentTemplateSelector="{TemplateBinding ItemTemplateSelector}"/>
                <TextBlock Text="▾" Foreground="#AAB5C2" HorizontalAlignment="Right" VerticalAlignment="Center" Margin="0,0,9,0"/>
              </Grid>
            </Border>
            <ToggleButton Focusable="False" Background="Transparent" BorderThickness="0"
                          IsChecked="{Binding IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}">
              <ToggleButton.Template><ControlTemplate TargetType="ToggleButton"><Border Background="Transparent"/></ControlTemplate></ToggleButton.Template>
            </ToggleButton>
            <Popup x:Name="PART_Popup" Placement="Bottom" IsOpen="{TemplateBinding IsDropDownOpen}" AllowsTransparency="True" Focusable="False" PopupAnimation="Fade">
              <Border Background="#0D131A" BorderBrush="#46576B" BorderThickness="1" CornerRadius="3" MinWidth="220" MaxHeight="360">
                <ScrollViewer CanContentScroll="True" VerticalScrollBarVisibility="Auto"><ItemsPresenter/></ScrollViewer>
              </Border>
            </Popup>
          </Grid>
          <ControlTemplate.Triggers>
            <Trigger Property="IsKeyboardFocusWithin" Value="True"><Setter TargetName="Chrome" Property="BorderBrush" Value="#3E8DFF"/></Trigger>
            <Trigger Property="IsMouseOver" Value="True"><Setter TargetName="Chrome" Property="BorderBrush" Value="#6AB9FF"/></Trigger>
            <Trigger Property="IsEnabled" Value="False"><Setter Property="Opacity" Value="0.45"/></Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>
  <Style x:Key="StarbindScrollBar" TargetType="ScrollBar">
    <Setter Property="Width" Value="9"/>
    <Setter Property="Background" Value="#0D131A"/>
    <Setter Property="Foreground" Value="#46576B"/>
  </Style>
</ResourceDictionary>
""";
        Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Parse(xaml));
    }

    private static Border Card() => new()
    {
        Background = Panel,
        BorderBrush = Border,
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(6)
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
            Padding = new Thickness(10, 8, 10, 8),
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
            Padding = new Thickness(14, 10, 14, 10),
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
        button.Padding = new Thickness(11, 8, 11, 8);
        button.Margin = new Thickness(4, 0, 0, 0);
        button.Cursor = Cursors.Hand;
        button.Click += click;
    }

    private void StyleCombo(ComboBox combo)
    {
        combo.Style = (Style)Resources["StarbindCombo"];
        combo.MinHeight = 31;
    }

    private static void StyleTextBox(TextBox box)
    {
        box.Background = Field;
        box.Foreground = Text;
        box.BorderBrush = Border2;
        box.CaretBrush = Text;
        box.Padding = new Thickness(8, 6, 8, 6);
    }

    private static void StyleTree(TreeView tree)
    {
        tree.Background = Field;
        tree.Foreground = Text;
        tree.BorderBrush = Border;
        tree.Padding = new Thickness(5);
        ScrollViewer.SetHorizontalScrollBarVisibility(tree, ScrollBarVisibility.Disabled);
        ScrollViewer.SetVerticalScrollBarVisibility(tree, ScrollBarVisibility.Auto);
    }

    private static void StyleList(ListBox list)
    {
        list.Background = Field;
        list.Foreground = Text;
        list.BorderBrush = Border;
        list.Padding = new Thickness(4);
        ScrollViewer.SetHorizontalScrollBarVisibility(list, ScrollBarVisibility.Disabled);
        ScrollViewer.SetVerticalScrollBarVisibility(list, ScrollBarVisibility.Auto);
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
        panel.SetValue(FrameworkElement.MarginProperty, new Thickness(3, 6, 3, 6));
        var icon = new FrameworkElementFactory(typeof(TextBlock));
        icon.SetBinding(TextBlock.TextProperty, new Binding(nameof(StarbindWarning.Icon)));
        icon.SetBinding(TextBlock.ForegroundProperty, new Binding(nameof(StarbindWarning.Color)));
        icon.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
        icon.SetValue(FrameworkElement.WidthProperty, 22d);
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

    internal static Button DialogButton(string text, Brush background) => new()
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
