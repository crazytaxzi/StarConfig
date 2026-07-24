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
  <Style TargetType="ScrollBar">
    <Setter Property="SnapsToDevicePixels" Value="True"/>
    <Setter Property="Width" Value="8"/>
    <Setter Property="Height" Value="8"/>
    <Setter Property="Background" Value="#080C12"/>
    <Setter Property="Template">
      <Setter.Value>
        <ControlTemplate TargetType="ScrollBar">
          <Grid Background="{TemplateBinding Background}" Margin="1">
            <Track x:Name="PART_Track" Orientation="{TemplateBinding Orientation}" IsDirectionReversed="True" Focusable="False">
              <Track.DecreaseRepeatButton>
                <RepeatButton Focusable="False" Opacity="0"/>
              </Track.DecreaseRepeatButton>
              <Track.Thumb>
                <Thumb x:Name="ScrollThumb" Background="#3B4D61" MinWidth="24" MinHeight="24">
                  <Thumb.Template>
                    <ControlTemplate TargetType="Thumb">
                      <Border x:Name="ThumbBody" Background="{TemplateBinding Background}" CornerRadius="4" Margin="1"/>
                      <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True"><Setter TargetName="ThumbBody" Property="Background" Value="#52749A"/></Trigger>
                        <Trigger Property="IsDragging" Value="True"><Setter TargetName="ThumbBody" Property="Background" Value="#3E8DFF"/></Trigger>
                      </ControlTemplate.Triggers>
                    </ControlTemplate>
                  </Thumb.Template>
                </Thumb>
              </Track.Thumb>
              <Track.IncreaseRepeatButton>
                <RepeatButton Focusable="False" Opacity="0"/>
              </Track.IncreaseRepeatButton>
            </Track>
          </Grid>
          <ControlTemplate.Triggers>
            <Trigger Property="Orientation" Value="Horizontal"><Setter TargetName="PART_Track" Property="IsDirectionReversed" Value="False"/></Trigger>
            <Trigger Property="IsEnabled" Value="False"><Setter Property="Opacity" Value="0.35"/></Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>
  <Style TargetType="CheckBox">
    <Setter Property="Foreground" Value="#AAB5C2"/>
    <Setter Property="VerticalContentAlignment" Value="Center"/>
    <Setter Property="Template">
      <Setter.Value>
        <ControlTemplate TargetType="CheckBox">
          <Grid>
            <Grid.ColumnDefinitions><ColumnDefinition Width="Auto"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
            <Border x:Name="Box" Width="14" Height="14" Background="#0D131A" BorderBrush="#46576B" BorderThickness="1" CornerRadius="2" VerticalAlignment="Center">
              <TextBlock x:Name="Mark" Text="✓" Foreground="#6AB9FF" FontSize="11" FontWeight="Bold" HorizontalAlignment="Center" VerticalAlignment="Center" Visibility="Collapsed"/>
            </Border>
            <ContentPresenter Grid.Column="1" Margin="7,0,0,0" VerticalAlignment="Center"/>
          </Grid>
          <ControlTemplate.Triggers>
            <Trigger Property="IsChecked" Value="True"><Setter TargetName="Mark" Property="Visibility" Value="Visible"/><Setter TargetName="Box" Property="BorderBrush" Value="#3E8DFF"/><Setter TargetName="Box" Property="Background" Value="#173A67"/></Trigger>
            <Trigger Property="IsMouseOver" Value="True"><Setter TargetName="Box" Property="BorderBrush" Value="#6AB9FF"/></Trigger>
            <Trigger Property="IsEnabled" Value="False"><Setter Property="Opacity" Value="0.45"/></Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
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
        combo.BorderBrush = Border2;
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
