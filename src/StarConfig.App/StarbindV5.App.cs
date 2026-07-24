using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Threading;
using ShapeEllipse = System.Windows.Shapes.Ellipse;
using ShapeLine = System.Windows.Shapes.Line;
using ShapePath = System.Windows.Shapes.Path;
using ShapePolygon = System.Windows.Shapes.Polygon;
using ShapePolyline = System.Windows.Shapes.Polyline;
using ShapeRectangle = System.Windows.Shapes.Rectangle;

namespace StarConfig;

public static class StarbindV5Program
{
    [STAThread]
    public static void Main()
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
        app.Run(new StarbindV5Window());
    }
}

public sealed partial class StarbindV5Window : Window
{
    internal static readonly Brush Bg = Paint("#080C12");
    internal static readonly Brush Rail = Paint("#0C121A");
    internal static readonly Brush Panel = Paint("#121922");
    internal static readonly Brush Panel2 = Paint("#171F29");
    internal static readonly Brush Field = Paint("#0D131A");
    internal static readonly Brush Border = Paint("#303B49");
    internal static readonly Brush Border2 = Paint("#46576B");
    internal static readonly Brush Blue = Paint("#3E8DFF");
    internal static readonly Brush BlueDim = Paint("#173A67");
    internal static readonly Brush Cyan = Paint("#6AB9FF");
    internal static readonly Brush Green = Paint("#4CD46B");
    internal static readonly Brush Amber = Paint("#F3B34B");
    internal static readonly Brush Red = Paint("#F06868");
    internal static readonly Brush Text = Paint("#F4F7FB");
    internal static readonly Brush Muted = Paint("#AAB5C2");
    internal static readonly Brush Faint = Paint("#6E7C8C");

    private static readonly string[] ContextOrder = ["Flight", "Vehicle", "On Foot", "EVA", "Turret", "Mining", "Salvage", "General"];

    private readonly StarbindProfileService _profiles = new();
    private readonly JoystickService _joysticks = new();
    private readonly StarbindV5SettingsStore _settingsStore = new();
    private readonly HardwareDefinitionService _hardware = new();
    private readonly DispatcherTimer _inputTimer;
    private readonly Dictionary<int, JoystickSnapshot> _captureBaselines = [];
    private readonly Dictionary<string, ControlBindingPlan> _pendingPlans = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AxisTuningChange> _axisTunings = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PlannedStateBinding> _visibleStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ComboBox> _statePickers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CheckBox> _stateChecks = new(StringComparer.OrdinalIgnoreCase);

    private StarbindV5Settings _settings;
    private StarbindProfile? _profile;
    private IReadOnlyList<InputDevice> _detectedDevices = [];
    private StarbindDevice? _selectedDevice;
    private HardwareTemplate? _selectedTemplate;
    private StarbindControl? _selectedControl;
    private StarbindAction? _selectedAction;
    private bool _capturing;
    private bool _suppressUi;
    private double _zoom = 1.0;
    private DateTime? _lastSaved;

    private readonly ComboBox _profilePicker = new();
    private readonly TextBlock _channelText = new();
    private readonly TextBlock _deviceCountText = new();
    private readonly StackPanel _deviceCards = new() { Orientation = Orientation.Horizontal };
    private readonly ComboBox _devicePicker = new();
    private readonly TreeView _controlTree = new();
    private readonly Grid _deviceCanvasHost = new();
    private readonly TextBlock _deviceTitle = new();
    private readonly TextBlock _deviceSubtitle = new();
    private readonly TextBlock _selectedControlName = new();
    private readonly TextBlock _selectedControlInput = new();
    private readonly TextBlock _selectedControlType = new();
    private readonly TextBlock _selectedControlAssignmentCount = new();
    private readonly Canvas _responseGraph = new();
    private readonly ComboBox _deadzonePicker = new();
    private readonly ComboBox _curvePicker = new();
    private readonly ComboBox _primaryActionPicker = new();
    private readonly StackPanel _stateRows = new();
    private readonly TextBlock _actionDescription = new();
    private readonly StackPanel _similarActions = new();
    private readonly ListBox _warnings = new();
    private readonly TextBox _actionSearch = new();
    private readonly TreeView _actionTree = new();
    private readonly TextBlock _browserActionTitle = new();
    private readonly TextBlock _browserActionBody = new();
    private readonly WrapPanel _stateOverview = new() { Orientation = Orientation.Horizontal };
    private readonly TextBlock _status = new();
    private readonly TextBlock _savedText = new();
    private readonly Button _listenButton = new();
    private readonly Button _testButton = new();
    private readonly Button _view3DButton = new();
    private readonly Button _applyButton = new();
    private readonly Button _compareButton = new();
    private readonly Button _resolveAllButton = new();
    private readonly Button _saveLaunchButton = new();
    private readonly Button _saveMenuButton = new();
    private readonly CheckBox _showUnassigned = new() { Content = "Show unassigned only", Foreground = Muted, VerticalAlignment = VerticalAlignment.Center };
    private readonly ComboBox _zoomPicker = new();

    public StarbindV5Window()
    {
        _settings = _settingsStore.Load();
        Title = "Starbind 0.6 - Star Citizen Control Mapper";
        Width = 1720;
        Height = 1040;
        MinWidth = 1360;
        MinHeight = 820;
        Background = Bg;
        Foreground = Text;
        FontFamily = new FontFamily("Segoe UI");
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        InstallControlStyles();
        Resources[typeof(ComboBox)] = Resources["StarbindCombo"];
        Resources[typeof(ComboBoxItem)] = Resources["StarbindComboItem"];
        Content = BuildLayout();

        PreviewKeyDown += CaptureKeyboard;
        PreviewMouseDown += CaptureMouse;
        _inputTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
        _inputTimer.Tick += InputTimerTick;
        Loaded += (_, _) => InitializeApplication();
        Closing += (_, _) => _settingsStore.Save(_settings);
    }

    internal static SolidColorBrush Paint(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
}
