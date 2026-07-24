global using System.Windows.Input;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StarConfig;

internal sealed class ShapeEllipse : Border
{
    public Brush? Fill
    {
        get => Background;
        set => Background = value;
    }

    public Brush? Stroke
    {
        get => BorderBrush;
        set => BorderBrush = value;
    }

    public double StrokeThickness
    {
        get => BorderThickness.Left;
        set => BorderThickness = new Thickness(value);
    }

    public ShapeEllipse()
    {
        CornerRadius = new CornerRadius(999);
    }
}
