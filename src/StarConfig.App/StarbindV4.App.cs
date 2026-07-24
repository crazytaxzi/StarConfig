using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace StarConfig;

public static class StarbindProgram
{
    [STAThread]
    public static void Main()
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
        app.Run(new StarbindWindow());
    }
}

public sealed partial class StarbindWindow : Window
{
    internal static readonly Brush Bg = Brush("#080C12");
    internal static readonly Brush Rail = Brush("#0C121A");
    internal static readonly Brush Panel = Brush("#121922");
    internal static readonly Brush Panel2 = Brush("#171F29");
    internal static readonly Brush Field = Brush("#0D131A");
    internal static readonly Brush Border = Brush("#303B49");
    internal static readonly Brush Border2 = Brush("#46576B");
    internal static readonly Brush Blue = Brush("#3E8DFF");
    internal static readonly Brush BlueDim = Brush("#173A67");
    internal static readonly Brush Cyan = Brush("#6AB9FF");
    internal static readonly Brush Green = Brush("#4CD46B");
    internal static readonly Brush Amber = Brush("#F3B34B");
    internal static readonly Brush Red = Brush("#F06868");
    internal static readonly Brush Text = Brush("#F4F7FB");
    internal static readonly Brush Muted = Brush("#AAB5C2");
    internal static readonly Brush Faint = Brush("#6E7C8C");

    private static readonly string[] Contexts = ["Flight", "Vehicle", "On Foot", "EVA", "Turret", "Mining", "Salvage", "General"];

    private readonly StarbindProfileService _profiles = new();
    private readonly JoystickService _joysticks = new();
    private readonly StarbindPreferenceStore _preferences = new();
    private readonly DispatcherTimer _inputTimer;
    private readonly Dictionary<int, JoystickSnapshot> _captureBaselines = [];
    private readonly Dictionary<string, StateAssignment> _stateAssignments = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ComboBox> _statePickers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CheckBox> _stateChecks = new(StringComparer.OrdinalIgnoreCase);

    private StarbindProfile? _profile;
    private IReadOnlyList<InputDevice> _detectedDevices = [];
    private StarbindDevice? _selectedDevice;
    private StarbindControl? _selectedControl;
    private StarbindAction? _selectedAction;
    private bool _capturing;
    private bool _suppressSelectionEvents;
    private DateTime? _lastSaved;

    private readonly ComboBox _profilePicker = new();
    private readonly TextBlock _channelText = new();
    private readonly TextBlock _deviceCountText = new();
    private readonly StackPanel _deviceCards = new() { Orientation = Orientation.Horizontal };
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
    private readonly StackPanel _stateOverview = new() { Orientation = Orientation.Horizontal };
    private readonly TextBlock _status = new();
    private readonly TextBlock _savedText = new();
    private readonly Button _listenButton = new();
    private readonly Button _applyButton = new();
    private readonly Button _saveLaunchButton = new();
    private readonly CheckBox _showUnassigned = new() { Content = "Show Unassigned Only", Foreground = Muted, VerticalAlignment = VerticalAlignment.Center };

    public StarbindWindow()
    {
        Title = "Starbind - Star Citizen Control Mapper";
        Width = 1600;
        Height = 1000;
        MinWidth = 1280;
        MinHeight = 800;
        Background = Bg;
        Foreground = Text;
        FontFamily = new FontFamily("Segoe UI");
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Content = BuildLayout();

        PreviewKeyDown += CaptureKeyboard;
        PreviewMouseDown += CaptureMouse;
        _inputTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _inputTimer.Tick += InputTimerTick;
        Loaded += (_, _) => Initialize();
    }

    private static SolidColorBrush Brush(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
}
