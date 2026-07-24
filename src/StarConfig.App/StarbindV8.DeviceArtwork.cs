using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ShapeEllipse = System.Windows.Shapes.Ellipse;
using ShapeLine = System.Windows.Shapes.Line;
using ShapePath = System.Windows.Shapes.Path;
using ShapeRectangle = System.Windows.Shapes.Rectangle;

namespace StarConfig;

public sealed partial class StarbindV5Window
{
    private bool _v8ArtworkRefreshInProgress;

    private void RefreshCorrectedDeviceArtwork()
    {
        if (_v8ArtworkRefreshInProgress || _selectedDevice is null || _selectedTemplate is null) return;
        _v8ArtworkRefreshInProgress = true;
        try
        {
            var viewbox = VisualDescendants<Viewbox>(_deviceCanvasHost).FirstOrDefault();
            if (viewbox is null) return;

            if (_selectedTemplate.Family == HardwareFamily.Keyboard)
            {
                var marker = $"V8_KEYBOARD:{_selectedDevice.InputPrefix}:{_selectedControl?.Input}";
                if (viewbox.Child is FrameworkElement existingKeyboard && existingKeyboard.Uid == marker) return;
                var keyboard = BuildInteractiveKeyboardSurface();
                keyboard.Uid = marker;
                viewbox.Child = keyboard;
                return;
            }

            if (viewbox.Child is not Canvas canvas) return;
            var existing = canvas.Children.Cast<UIElement>().FirstOrDefault(element =>
            {
                var left = Canvas.GetLeft(element);
                var top = Canvas.GetTop(element);
                return !double.IsNaN(left) && !double.IsNaN(top) && Math.Abs(left - 260) < .5 && Math.Abs(top - 10) < .5;
            });
            if (existing is FrameworkElement current && current.Uid.StartsWith("V8_ART:", StringComparison.Ordinal)) return;
            if (existing is not null) canvas.Children.Remove(existing);

            var artwork = StarbindDeviceArtworkV8.Build(_selectedTemplate, _selectedDevice, 420, 475, Panel2, Border2, Cyan, Muted);
            artwork.Uid = $"V8_ART:{_selectedTemplate.Id}:{_selectedDevice.ProductName}";
            Canvas.SetLeft(artwork, 260);
            Canvas.SetTop(artwork, 10);
            canvas.Children.Insert(0, artwork);
        }
        finally { _v8ArtworkRefreshInProgress = false; }
    }

    private void RefreshCorrectedDeviceThumbnails()
    {
        if (_v8ArtworkRefreshInProgress) return;
        _v8ArtworkRefreshInProgress = true;
        try
        {
            foreach (var card in _deviceCards.Children.OfType<Button>())
            {
                if (card.Tag is not StarbindDevice device || card.Content is not Grid grid) continue;
                var template = _hardware.Resolve(device, _settings);
                var current = grid.Children.Cast<UIElement>().FirstOrDefault(element => Grid.GetColumn(element) == 0);
                var marker = $"V8_THUMB:{template.Id}:{device.ProductName}";
                if (current is FrameworkElement currentElement && currentElement.Uid == marker) continue;
                if (current is not null) grid.Children.Remove(current);
                var art = StarbindDeviceArtworkV8.Build(template, device, 160, 160, Field, Border2, Cyan, Muted);
                var thumb = new Viewbox { Width = 46, Height = 46, Stretch = Stretch.Uniform, Child = art, Uid = marker };
                Grid.SetColumn(thumb, 0);
                grid.Children.Insert(0, thumb);
            }
        }
        finally { _v8ArtworkRefreshInProgress = false; }
    }

    private FrameworkElement BuildInteractiveKeyboardSurface()
    {
        if (_selectedDevice is null) return new Grid();
        var canvas = new Canvas { Width = 1120, Height = 430, Background = Paint("#090E15") };
        var title = new TextBlock
        {
            Text = "FULL KEYBOARD - click a key or drag an action onto it",
            Foreground = Muted,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold
        };
        Canvas.SetLeft(title, 20);
        Canvas.SetTop(title, 10);
        canvas.Children.Add(title);

        foreach (var key in StarbindKeyboardCatalog.Keys)
        {
            var control = KeyboardControl(key);
            if (_showUnassigned.IsChecked == true && EffectiveAssignments(control).Any()) continue;
            var assignment = EffectiveAssignments(control).FirstOrDefault();
            var selected = control.Input.Equals(_selectedControl?.Input, StringComparison.OrdinalIgnoreCase);
            var button = new Button
            {
                Width = key.Width,
                Height = key.Height,
                Background = selected ? BlueDim : assignment is null ? Paint("#151D27") : Paint("#183225"),
                BorderBrush = selected ? Blue : assignment is null ? Border2 : Green,
                BorderThickness = new Thickness(selected ? 2 : 1),
                Foreground = Text,
                Padding = new Thickness(3),
                Cursor = Cursors.Hand,
                Tag = control,
                AllowDrop = true,
                ToolTip = assignment is null
                    ? $"{control.DisplayName}\n{control.Input}\nUnassigned"
                    : $"{control.DisplayName}\n{control.Input}\n{assignment.Context}: {assignment.DisplayName}"
            };
            var words = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            words.Children.Add(new TextBlock
            {
                Text = key.Label,
                FontSize = key.Width < 48 ? 9 : 10,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            if (assignment is not null && key.Width >= 62)
            {
                words.Children.Add(new TextBlock
                {
                    Text = assignment.DisplayName,
                    Foreground = Green,
                    FontSize = 7.5,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = key.Width - 8
                });
            }
            button.Content = words;
            button.Click += (_, _) => SelectControl(control);
            button.PreviewDragOver += MappingTargetDragOver;
            button.Drop += MappingTargetDrop;
            Canvas.SetLeft(button, key.X);
            Canvas.SetTop(button, key.Y);
            canvas.Children.Add(button);
        }
        return canvas;
    }

    private StarbindControl KeyboardControl(StarbindKeyboardKey key)
    {
        var device = _selectedDevice ?? new StarbindDevice(1, "Keyboard", StarbindDeviceKind.Keyboard, 0, 0, true);
        return new StarbindControl($"{device.InputPrefix}_{key.Suffix}", key.Name, StarbindControlKind.Key, device.Instance, device.InputPrefix, key.Suffix.ToUpperInvariant());
    }

    private void BuildCorrectedKeyboardControlTree()
    {
        if (_selectedDevice is null || _selectedTemplate?.Family != HardwareFamily.Keyboard) return;
        if (_controlTree.Items.OfType<TreeViewItem>().Any(item => Equals(item.Tag, "V8_KEYBOARD_ROOT"))) return;
        _controlTree.Items.Clear();
        var first = true;
        foreach (var group in StarbindKeyboardCatalog.Keys.GroupBy(key => key.Group))
        {
            var root = TreeGroup(group.Key.ToUpperInvariant());
            if (first) { root.Tag = "V8_KEYBOARD_ROOT"; first = false; }
            foreach (var key in group)
            {
                var control = KeyboardControl(key);
                var count = EffectiveAssignments(control).Count();
                if (_showUnassigned.IsChecked == true && count > 0) continue;
                var row = new DockPanel { LastChildFill = true };
                var badge = new TextBlock
                {
                    Text = count == 0 ? string.Empty : count == 1 ? "1 bind" : $"{count} binds",
                    Foreground = count == 0 ? Faint : Green,
                    FontSize = 9,
                    Margin = new Thickness(8, 0, 0, 0)
                };
                DockPanel.SetDock(badge, Dock.Right);
                row.Children.Add(badge);
                row.Children.Add(new TextBlock { Text = key.Name, Foreground = Text });
                root.Items.Add(new TreeViewItem { Header = row, Tag = control, Foreground = Text, Padding = new Thickness(3) });
            }
            root.IsExpanded = group.Key is "Movement Cluster" or "Main Keys";
            _controlTree.Items.Add(root);
        }
    }
}

internal sealed record StarbindKeyboardKey(string Suffix, string Label, string Name, string Group, double X, double Y, double Width, double Height);

internal static class StarbindKeyboardCatalog
{
    public static IReadOnlyList<StarbindKeyboardKey> Keys { get; } = Build();

    private static IReadOnlyList<StarbindKeyboardKey> Build()
    {
        var keys = new List<StarbindKeyboardKey>();
        const double unit = 48;
        const double gap = 5;
        const double height = 48;

        void AddRow(string group, double x, double y, params (string Suffix, string Label, string Name, double Units)[] row)
        {
            foreach (var item in row)
            {
                var width = item.Units * unit + (item.Units - 1) * gap;
                keys.Add(new StarbindKeyboardKey(item.Suffix, item.Label, item.Name, group, x, y, width, height));
                x += width + gap;
            }
        }

        AddRow("Function Keys", 20, 42,
            ("escape", "Esc", "Escape", 1), ("f1", "F1", "F1", 1), ("f2", "F2", "F2", 1), ("f3", "F3", "F3", 1), ("f4", "F4", "F4", 1),
            ("f5", "F5", "F5", 1), ("f6", "F6", "F6", 1), ("f7", "F7", "F7", 1), ("f8", "F8", "F8", 1),
            ("f9", "F9", "F9", 1), ("f10", "F10", "F10", 1), ("f11", "F11", "F11", 1), ("f12", "F12", "F12", 1));
        AddRow("Main Keys", 20, 100,
            ("tilde", "`", "Tilde", 1), ("1", "1", "1", 1), ("2", "2", "2", 1), ("3", "3", "3", 1), ("4", "4", "4", 1), ("5", "5", "5", 1), ("6", "6", "6", 1),
            ("7", "7", "7", 1), ("8", "8", "8", 1), ("9", "9", "9", 1), ("0", "0", "0", 1), ("minus", "-", "Minus", 1), ("equals", "=", "Equals", 1), ("backspace", "Backspace", "Backspace", 2));
        AddRow("Main Keys", 20, 153,
            ("tab", "Tab", "Tab", 1.5), ("q", "Q", "Q", 1), ("w", "W", "W", 1), ("e", "E", "E", 1), ("r", "R", "R", 1), ("t", "T", "T", 1), ("y", "Y", "Y", 1),
            ("u", "U", "U", 1), ("i", "I", "I", 1), ("o", "O", "O", 1), ("p", "P", "P", 1), ("lbracket", "[", "Left Bracket", 1), ("rbracket", "]", "Right Bracket", 1), ("backslash", "\\", "Backslash", 1.5));
        AddRow("Main Keys", 20, 206,
            ("capslock", "Caps", "Caps Lock", 1.75), ("a", "A", "A", 1), ("s", "S", "S", 1), ("d", "D", "D", 1), ("f", "F", "F", 1), ("g", "G", "G", 1),
            ("h", "H", "H", 1), ("j", "J", "J", 1), ("k", "K", "K", 1), ("l", "L", "L", 1), ("semicolon", ";", "Semicolon", 1), ("apostrophe", "'", "Apostrophe", 1), ("enter", "Enter", "Enter", 2.25));
        AddRow("Main Keys", 20, 259,
            ("lshift", "Shift", "Left Shift", 2.25), ("z", "Z", "Z", 1), ("x", "X", "X", 1), ("c", "C", "C", 1), ("v", "V", "V", 1), ("b", "B", "B", 1),
            ("n", "N", "N", 1), ("m", "M", "M", 1), ("comma", ",", "Comma", 1), ("period", ".", "Period", 1), ("slash", "/", "Slash", 1), ("rshift", "Shift", "Right Shift", 2.75));
        AddRow("Modifiers", 20, 312,
            ("lctrl", "Ctrl", "Left Control", 1.35), ("lwin", "Win", "Left Windows", 1.1), ("lalt", "Alt", "Left Alt", 1.1), ("space", "Space", "Space", 6.15),
            ("ralt", "Alt", "Right Alt", 1.1), ("rwin", "Win", "Right Windows", 1.1), ("apps", "Menu", "Application Menu", 1.1), ("rctrl", "Ctrl", "Right Control", 1.35));
        AddRow("Navigation", 785, 100, ("insert", "Ins", "Insert", 1), ("home", "Home", "Home", 1), ("pgup", "PgUp", "Page Up", 1));
        AddRow("Navigation", 785, 153, ("delete", "Del", "Delete", 1), ("end", "End", "End", 1), ("pgdn", "PgDn", "Page Down", 1));
        AddRow("Movement Cluster", 838, 259, ("up", "Up", "Arrow Up", 1));
        AddRow("Movement Cluster", 785, 312, ("left", "Left", "Arrow Left", 1), ("down", "Down", "Arrow Down", 1), ("right", "Right", "Arrow Right", 1));
        AddRow("Numpad", 958, 42, ("numlock", "Num", "Num Lock", 1), ("np_divide", "/", "Numpad Divide", 1), ("np_multiply", "*", "Numpad Multiply", 1));
        AddRow("Numpad", 958, 100, ("np_7", "7", "Numpad 7", 1), ("np_8", "8", "Numpad 8", 1), ("np_9", "9", "Numpad 9", 1));
        AddRow("Numpad", 958, 153, ("np_4", "4", "Numpad 4", 1), ("np_5", "5", "Numpad 5", 1), ("np_6", "6", "Numpad 6", 1));
        AddRow("Numpad", 958, 206, ("np_1", "1", "Numpad 1", 1), ("np_2", "2", "Numpad 2", 1), ("np_3", "3", "Numpad 3", 1));
        AddRow("Numpad", 958, 259, ("np_0", "0", "Numpad 0", 2), ("np_decimal", ".", "Numpad Decimal", 1));
        AddRow("Numpad", 958, 312, ("np_subtract", "-", "Numpad Subtract", 1), ("np_add", "+", "Numpad Add", 1), ("np_enter", "Enter", "Numpad Enter", 1));
        return keys;
    }
}

internal static class StarbindDeviceArtworkV8
{
    public static FrameworkElement Build(HardwareTemplate template, StarbindDevice device, double width, double height, Brush fill, Brush stroke, Brush accent, Brush muted)
    {
        var canvas = new Canvas { Width = width, Height = height, Background = Brushes.Transparent };
        switch (template.Id)
        {
            case "vkb-gladiator": DrawVkbGladiator(canvas, width, height, fill, stroke, accent, device.ProductName); break;
            case "vkb-stecs": DrawVkbStecs(canvas, width, height, fill, stroke, accent); break;
            case "logitech-rudder": DrawRudder(canvas, width, height, fill, stroke, accent); break;
            case "t16000": DrawT16000(canvas, width, height, fill, stroke, accent); break;
            case "virpil-stick": DrawVirpil(canvas, width, height, fill, stroke, accent, device.ProductName); break;
            case "mouse": DrawMouse(canvas, width, height, fill, stroke, accent); break;
            case "gamepad": DrawGamepad(canvas, width, height, fill, stroke, accent); break;
            case "keyboard": DrawKeyboard(canvas, width, height, fill, stroke, accent); break;
            default:
                if (template.Family == HardwareFamily.Throttle) DrawVkbStecs(canvas, width, height, fill, stroke, accent);
                else if (template.Family == HardwareFamily.Pedals) DrawRudder(canvas, width, height, fill, stroke, accent);
                else DrawGenericStick(canvas, width, height, fill, stroke, accent);
                break;
        }
        var plate = new TextBlock { Text = template.DisplayName.ToUpperInvariant(), Foreground = muted, FontSize = Math.Max(8, Math.Min(12, width / 35)), Width = width, TextAlignment = TextAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
        Canvas.SetTop(plate, height * .94);
        canvas.Children.Add(plate);
        return canvas;
    }

    private static void DrawVkbGladiator(Canvas c, double w, double h, Brush fill, Brush stroke, Brush accent, string name)
    {
        var left = name.Contains(" EVO L", StringComparison.OrdinalIgnoreCase) || name.Contains("LEFT", StringComparison.OrdinalIgnoreCase);
        Rounded(c, w*.16, h*.72, w*.68, h*.19, fill, stroke, 18);
        Rounded(c, left ? w*.68 : w*.20, h*.59, w*.09, h*.19, Paint("#252E39"), stroke, 5);
        for (var i=0;i<4;i++) Circle(c, w*(.27+i*.15), h*.84, w*.021, i==1?accent:stroke, stroke);
        var grip = new ShapePath { Data = Geometry.Parse(left
            ? $"M {w*.54},{h*.72} C {w*.44},{h*.59} {w*.41},{h*.42} {w*.44},{h*.22} C {w*.46},{h*.08} {w*.58},{h*.06} {w*.68},{h*.16} L {w*.71},{h*.31} L {w*.64},{h*.48} L {w*.67},{h*.69} Z"
            : $"M {w*.46},{h*.72} C {w*.56},{h*.59} {w*.59},{h*.42} {w*.56},{h*.22} C {w*.54},{h*.08} {w*.42},{h*.06} {w*.32},{h*.16} L {w*.29},{h*.31} L {w*.36},{h*.48} L {w*.33},{h*.69} Z"), Fill = Paint("#2B3540"), Stroke = stroke, StrokeThickness = 3 };
        c.Children.Add(grip);
        Circle(c, left?w*.58:w*.42, h*.22, w*.035, accent, stroke);
        Circle(c, left?w*.63:w*.37, h*.32, w*.026, Paint("#C64949"), stroke);
        Line(c, left?w*.50:w*.50, h*.30, left?w*.42:w*.58, h*.38, accent, 5);
    }

    private static void DrawVkbStecs(Canvas c, double w, double h, Brush fill, Brush stroke, Brush accent)
    {
        Rounded(c,w*.10,h*.56,w*.80,h*.34,fill,stroke,20);
        for(var i=0;i<4;i++) Circle(c,w*(.22+i*.18),h*.82,w*.023,i==2?accent:stroke,stroke);
        Rounded(c,w*.25,h*.12,w*.22,h*.52,Paint("#29343F"),stroke,18,-8);
        Rounded(c,w*.53,h*.08,w*.22,h*.56,Paint("#29343F"),stroke,18,8);
        Rounded(c,w*.22,h*.05,w*.28,h*.18,Paint("#202933"),stroke,14,-8);
        Rounded(c,w*.50,h*.02,w*.28,h*.18,Paint("#202933"),stroke,14,8);
        Circle(c,w*.36,h*.13,w*.03,accent,stroke); Circle(c,w*.65,h*.10,w*.03,Paint("#C64949"),stroke);
    }

    private static void DrawRudder(Canvas c,double w,double h,Brush fill,Brush stroke,Brush accent)
    {
        Rounded(c,w*.08,h*.66,w*.84,h*.18,fill,stroke,15);
        Line(c,w*.25,h*.64,w*.75,h*.64,accent,5);
        Rounded(c,w*.13,h*.17,w*.29,h*.49,Paint("#27313C"),stroke,12,-8);
        Rounded(c,w*.58,h*.17,w*.29,h*.49,Paint("#27313C"),stroke,12,8);
        for(var i=0;i<5;i++){ Line(c,w*.19,h*(.26+i*.07),w*.38,h*(.24+i*.07),stroke,2); Line(c,w*.62,h*(.24+i*.07),w*.81,h*(.26+i*.07),stroke,2); }
    }

    private static void DrawT16000(Canvas c,double w,double h,Brush fill,Brush stroke,Brush accent)
    {
        var orange=Paint("#F39A32");
        var basePlate=new ShapeEllipse{Width=w*.72,Height=h*.23,Fill=fill,Stroke=stroke,StrokeThickness=3}; Canvas.SetLeft(basePlate,w*.14);Canvas.SetTop(basePlate,h*.70);c.Children.Add(basePlate);
        for(var i=0;i<8;i++) Circle(c,w*(.20+i*.085),h*.84,w*.016,i is 3 or 4?orange:accent,stroke);
        Rounded(c,w*.44,h*.37,w*.12,h*.40,Paint("#27313B"),stroke,20);
        var grip=new ShapePath{Data=Geometry.Parse($"M {w*.43},{h*.43} C {w*.32},{h*.36} {w*.31},{h*.19} {w*.39},{h*.10} C {w*.47},{h*.03} {w*.61},{h*.08} {w*.64},{h*.18} L {w*.60},{h*.35} L {w*.56},{h*.47} Z"),Fill=Paint("#2B3540"),Stroke=stroke,StrokeThickness=3};c.Children.Add(grip);
        Circle(c,w*.50,h*.14,w*.04,orange,stroke); Line(c,w*.43,h*.22,w*.35,h*.29,orange,5);
    }

    private static void DrawVirpil(Canvas c,double w,double h,Brush fill,Brush stroke,Brush accent,string name)
    {
        var left=name.Contains("LEFT",StringComparison.OrdinalIgnoreCase);
        Rounded(c,w*.18,h*.72,w*.64,h*.20,Paint("#1A222C"),stroke,10);
        Rounded(c,w*.42,h*.59,w*.16,h*.18,fill,stroke,8);
        var grip=new ShapePath{Data=Geometry.Parse(left
            ? $"M {w*.55},{h*.64} C {w*.43},{h*.54} {w*.38},{h*.39} {w*.41},{h*.21} C {w*.43},{h*.07} {w*.55},{h*.04} {w*.67},{h*.13} L {w*.72},{h*.28} L {w*.64},{h*.43} L {w*.68},{h*.61} Z"
            : $"M {w*.45},{h*.64} C {w*.57},{h*.54} {w*.62},{h*.39} {w*.59},{h*.21} C {w*.57},{h*.07} {w*.45},{h*.04} {w*.33},{h*.13} L {w*.28},{h*.28} L {w*.36},{h*.43} L {w*.32},{h*.61} Z"),Fill=Paint("#303A45"),Stroke=stroke,StrokeThickness=3};c.Children.Add(grip);
        Circle(c,left?w*.59:w*.41,h*.17,w*.035,accent,stroke);Circle(c,left?w*.63:w*.37,h*.27,w*.026,Paint("#C64949"),stroke);Circle(c,left?w*.55:w*.45,h*.35,w*.026,accent,stroke);
    }

    private static void DrawGenericStick(Canvas c,double w,double h,Brush fill,Brush stroke,Brush accent)
    { Rounded(c,w*.20,h*.73,w*.60,h*.19,fill,stroke,16);Rounded(c,w*.43,h*.38,w*.14,h*.39,Paint("#28323D"),stroke,18);Rounded(c,w*.34,h*.09,w*.32,h*.34,Paint("#303A45"),stroke,22);Circle(c,w*.50,h*.20,w*.045,accent,stroke); }

    private static void DrawMouse(Canvas c,double w,double h,Brush fill,Brush stroke,Brush accent)
    { Rounded(c,w*.29,h*.10,w*.42,h*.78,fill,stroke,Math.Min(w,h)*.18);Line(c,w*.50,h*.11,w*.50,h*.42,stroke,3);Rounded(c,w*.46,h*.24,w*.08,h*.18,accent,stroke,4); }

    private static void DrawGamepad(Canvas c,double w,double h,Brush fill,Brush stroke,Brush accent)
    {
        var body=new ShapePath{Data=Geometry.Parse($"M {w*.16},{h*.40} C {w*.18},{h*.18} {w*.37},{h*.16} {w*.50},{h*.30} C {w*.63},{h*.16} {w*.82},{h*.18} {w*.84},{h*.40} L {w*.77},{h*.78} C {w*.72},{h*.91} {w*.61},{h*.78} {w*.55},{h*.62} L {w*.45},{h*.62} C {w*.39},{h*.78} {w*.28},{h*.91} {w*.23},{h*.78} Z"),Fill=fill,Stroke=stroke,StrokeThickness=3};c.Children.Add(body);
        Circle(c,w*.35,h*.49,w*.055,accent,stroke);Circle(c,w*.58,h*.60,w*.055,accent,stroke);Circle(c,w*.70,h*.37,w*.025,Paint("#C64949"),stroke);Line(c,w*.27,h*.36,w*.41,h*.36,stroke,5);Line(c,w*.34,h*.29,w*.34,h*.43,stroke,5);
    }

    private static void DrawKeyboard(Canvas c,double w,double h,Brush fill,Brush stroke,Brush accent)
    { Rounded(c,w*.05,h*.24,w*.90,h*.53,fill,stroke,10);for(var r=0;r<5;r++)for(var col=0;col<15;col++){var key=new ShapeRectangle{Width=w*.045,Height=h*.055,RadiusX=2,RadiusY=2,Fill=col is 3 or 7?accent:stroke,Opacity=.75};Canvas.SetLeft(key,w*.10+col*w*.054);Canvas.SetTop(key,h*.30+r*h*.075);c.Children.Add(key);} }

    private static Brush Paint(string color)=>new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    private static void Rounded(Canvas c,double x,double y,double w,double h,Brush fill,Brush stroke,double radius,double angle=0){var s=new ShapeRectangle{Width=w,Height=h,RadiusX=radius,RadiusY=radius,Fill=fill,Stroke=stroke,StrokeThickness=2,RenderTransform=new RotateTransform(angle,w/2,h/2)};Canvas.SetLeft(s,x);Canvas.SetTop(s,y);c.Children.Add(s);}
    private static void Circle(Canvas c,double x,double y,double r,Brush fill,Brush stroke){var s=new ShapeEllipse{Width=r*2,Height=r*2,Fill=fill,Stroke=stroke,StrokeThickness=1.5};Canvas.SetLeft(s,x-r);Canvas.SetTop(s,y-r);c.Children.Add(s);}
    private static void Line(Canvas c,double x1,double y1,double x2,double y2,Brush stroke,double thickness)=>c.Children.Add(new ShapeLine{X1=x1,Y1=y1,X2=x2,Y2=y2,Stroke=stroke,StrokeThickness=thickness,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round});
}
