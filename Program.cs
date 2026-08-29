using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;
using Drawing = System.Drawing;
using Color = System.Windows.Media.Color;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using Orientation = System.Windows.Controls.Orientation;
using FontFamily = System.Windows.Media.FontFamily;
using ToolTip = System.Windows.Controls.ToolTip;

namespace ResourcePins;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var overlay = new OverlayWindow();
        overlay.Show();
        app.Run();
    }
}

public class OverlayWindow : Window
{
    private readonly StackPanel _panel;
    private readonly DispatcherTimer _timer;
    private readonly WinForms.NotifyIcon _tray;
    private bool _testMode;

    private record PinDef(string Label, char Icon, Color Color, string[] Caps);

    // Glifos do Segoe MDL2 Assets: E714 Video, E720 Microphone, E81D Location, E7F4 TVMonitor
    private static readonly PinDef[] Pins =
    [
        new("Câmera", (char)0xE714, Color.FromRgb(46, 204, 113), ["webcam"]),
        new("Microfone", (char)0xE720, Color.FromRgb(231, 76, 60), ["microphone"]),
        new("Localização", (char)0xE81D, Color.FromRgb(52, 152, 219), ["location"]),
        new("Captura de tela", (char)0xE7F4, Color.FromRgb(155, 89, 182),
            ["graphicsCaptureProgrammatic", "graphicsCaptureWithoutBorder"]),
    ];

    private const double ActiveOpacity = 0.90; // aceso: em uso ativo agora
    private const double IdleOpacity = 0.22;   // apagado: recurso ocioso

    public OverlayWindow()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        SizeToContent = SizeToContent.WidthAndHeight;

        _panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2) };
        Content = _panel;

        // Ancorado no canto superior direito, estilo contador de FPS
        SizeChanged += (_, _) => AnchorToCorner();

        _tray = BuildTrayIcon();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Refresh();
        _timer.Start();
        Refresh();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        var ex = GetWindowLong(hwnd, GWL_EXSTYLE);
        // TOOLWINDOW: fora do alt-tab; NOACTIVATE: nunca rouba foco
        SetWindowLong(hwnd, GWL_EXSTYLE, ex | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
    }

    private void Refresh()
    {
        // cap -> apps usando AGORA (LastUsedTimeStart > 0 e LastUsedTimeStop == 0)
        var active = UsageMonitor.GetActiveUsages()
            .ToDictionary(u => u.Capability, u => u.Apps);

        _panel.Children.Clear();
        foreach (var pin in Pins)
        {
            var apps = pin.Caps
                .SelectMany(c => active.TryGetValue(c, out var a) ? a : [])
                .Distinct()
                .ToList();
            var isActive = apps.Count > 0 || _testMode;

            var tooltip = isActive
                ? $"{pin.Label} — EM USO\n{string.Join("\n", apps.Select(a => "• " + a))}".TrimEnd()
                : $"{pin.Label} — ocioso";

            _panel.Children.Add(BuildPin(pin.Icon, pin.Color, isActive, tooltip));
        }

        AnchorToCorner();

        // Reafirma o topmost — outras janelas topmost podem ter passado na frente
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }

    private static UIElement BuildPin(char icon, Color color, bool isActive, string tooltip)
    {
        var border = new Border
        {
            Width = 16,
            Height = 16,
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(2, 0, 2, 0),
            Opacity = isActive ? ActiveOpacity : IdleOpacity,
            Background = new SolidColorBrush(isActive ? color : Color.FromRgb(120, 120, 120)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            Child = new TextBlock
            {
                Text = icon.ToString(),
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 9,
                Foreground = Brushes.White,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
            },
            ToolTip = new ToolTip { Content = tooltip },
        };
        if (isActive)
        {
            // brilho na cor do pin pra "acender" de verdade
            border.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = color, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.9
            };
        }
        ToolTipService.SetInitialShowDelay(border, 150);
        return border;
    }

    private WinForms.NotifyIcon BuildTrayIcon()
    {
        var menu = new WinForms.ContextMenuStrip();
        var testItem = new WinForms.ToolStripMenuItem("Modo teste (acender todos os pins)")
        {
            CheckOnClick = true
        };
        testItem.CheckedChanged += (_, _) => { _testMode = testItem.Checked; Refresh(); };
        menu.Items.Add(testItem);
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("Sair", null, (_, _) =>
        {
            _tray!.Visible = false;
            Application.Current.Shutdown();
        });

        // Usa o ícone do próprio exe (guardião); escudo do sistema como fallback
        Drawing.Icon trayIcon;
        try
        {
            trayIcon = Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!)
                       ?? Drawing.SystemIcons.Shield;
        }
        catch { trayIcon = Drawing.SystemIcons.Shield; }

        return new WinForms.NotifyIcon
        {
            Icon = trayIcon,
            Text = "Resource Pins — guardião de câmera/mic/tela",
            Visible = true,
            ContextMenuStrip = menu,
        };
    }

    private void AnchorToCorner()
    {
        // Canto superior direito da tela primária, com uma folguinha
        Left = SystemParameters.PrimaryScreenWidth - ActualWidth - 8;
        Top = 4;
    }

    // --- Win32 ---
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private const uint SWP_NOMOVE = 0x0002, SWP_NOSIZE = 0x0001, SWP_NOACTIVATE = 0x0010;

    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int x, int y, int cx, int cy, uint flags);
}
