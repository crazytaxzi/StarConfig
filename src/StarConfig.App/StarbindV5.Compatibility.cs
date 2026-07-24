global using System.Windows.Input;

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

    public ShapeEllipse()
    {
        CornerRadius = new System.Windows.CornerRadius(999);
    }
}
