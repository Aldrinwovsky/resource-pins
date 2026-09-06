using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
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
    public static readonly string LogFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ResourcePins", "log.txt");

    [STAThread]
    public static void Main()
    {
        // Instancia unica: se ja ha um rodando, sai em silencio
        using var mutex = new System.Threading.Mutex(true, @"Local\ResourcePins", out var isNew);
        if (!isNew) return;

        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };

        // Nada de morte silenciosa: registra e segue de pe
        app.DispatcherUnhandledException += (_, e) =>
        {
            Log("Erro na UI: " + e.Exception);
            e.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log("Erro fatal: " + e.ExceptionObject);

        Log("Iniciado.");
        var overlay = new OverlayWindow();
        overlay.Show();
        app.Run();
    }

    public static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogFile)!);
            File.AppendAllText(LogFile,
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {message}{Environment.NewLine}");
        }
        catch { }
    }
}

public class OverlayWindow : Window
{
    private readonly StackPanel _panel;
    private readonly DispatcherTimer _timer;
    private readonly WinForms.NotifyIcon _tray;
    private readonly TrayPins _trayPins;
    private bool _testMode;
    private bool _showOverlay = Settings.GetBool("overlay", true);
    private bool _showTrayPins = Settings.GetBool("traypins", true);

    private record PinDef(string Key, string Label, char Icon, Color Color, string[] Caps);

    // Glifos do Segoe MDL2 Assets: E714 Video, E720 Microphone, E81D Location, E7F4 TVMonitor
    private static readonly PinDef[] Pins =
    [
        new("camera", "Camera", (char)0xE714, Color.FromRgb(46, 204, 113), ["webcam"]),
        new("mic", "Microfone", (char)0xE720, Color.FromRgb(231, 76, 60), ["microphone"]),
        new("local", "Localizacao", (char)0xE81D, Color.FromRgb(52, 152, 219), ["location"]),
        new("tela", "Captura de tela", (char)0xE7F4, Color.FromRgb(155, 89, 182),
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

        var menu = BuildMenu();
        _tray = BuildTrayIcon(menu);
        _trayPins = new TrayPins(
            Pins.Select(p => (p.Key, p.Label, p.Icon, ToGdi(p.Color))), menu);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => SafeRefresh();
        _timer.Start();
        SafeRefresh();
    }

    private static Drawing.Color ToGdi(Color c) => Drawing.Color.FromArgb(c.R, c.G, c.B);

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        var ex = GetWindowLong(hwnd, GWL_EXSTYLE);
        // TOOLWINDOW: fora do alt-tab; NOACTIVATE: nunca rouba foco
        SetWindowLong(hwnd, GWL_EXSTYLE, ex | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
    }

    /// <summary>Um tick que falha nao pode derrubar o app.</summary>
    private void SafeRefresh()
    {
        try { Refresh(); }
        catch (Exception ex) { Program.Log("Falha ao atualizar pins: " + ex); }
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
                ? $"{pin.Label} - EM USO\n{string.Join("\n", apps.Select(a => "- " + a))}".TrimEnd()
                : $"{pin.Label} - ocioso";

            _panel.Children.Add(BuildPin(pin.Icon, pin.Color, isActive, tooltip));

            // Barra de tarefas: o icone aparece so enquanto o recurso esta em uso
            var trayText = apps.Count > 0
                ? $"{pin.Label}: {string.Join(", ", apps)}"
                : $"{pin.Label} em uso";
            _trayPins.Update(pin.Key, isActive && _showTrayPins, trayText);
        }

        Visibility = _showOverlay ? Visibility.Visible : Visibility.Hidden;
        if (!_showOverlay) return;

        AnchorToCorner();

        // Reafirma o topmost - outras janelas topmost podem ter passado na frente
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
                Color = color,
                BlurRadius = 8,
                ShadowDepth = 0,
                Opacity = 0.9
            };
        }
        ToolTipService.SetInitialShowDelay(border, 150);
        return border;
    }

    private WinForms.ContextMenuStrip BuildMenu()
    {
        var menu = new WinForms.ContextMenuStrip();

        var trayItem = new WinForms.ToolStripMenuItem("Mostrar na barra de tarefas")
        {
            CheckOnClick = true,
            Checked = _showTrayPins,
        };
        trayItem.CheckedChanged += (_, _) =>
        {
            _showTrayPins = trayItem.Checked;
            Settings.SetBool("traypins", _showTrayPins);
            SafeRefresh();
        };
        menu.Items.Add(trayItem);

        var overlayItem = new WinForms.ToolStripMenuItem("Mostrar pins na tela (overlay)")
        {
            CheckOnClick = true,
            Checked = _showOverlay,
        };
        overlayItem.CheckedChanged += (_, _) =>
        {
            _showOverlay = overlayItem.Checked;
            Settings.SetBool("overlay", _showOverlay);
            SafeRefresh();
        };
        menu.Items.Add(overlayItem);

        var testItem = new WinForms.ToolStripMenuItem("Modo teste (acender todos os pins)")
        {
            CheckOnClick = true
        };
        testItem.CheckedChanged += (_, _) => { _testMode = testItem.Checked; SafeRefresh(); };
        menu.Items.Add(testItem);

        menu.Items.Add("Abrir log de diagnostico", null, (_, _) =>
        {
            Program.Log("Log aberto pelo usuario.");
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(Program.LogFile) { UseShellExecute = true });
        });

        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("Sair", null, (_, _) =>
        {
            _tray!.Visible = false;
            _trayPins?.Dispose();
            Application.Current.Shutdown();
        });
        return menu;
    }

    private WinForms.NotifyIcon BuildTrayIcon(WinForms.ContextMenuStrip menu)
    {
        // Usa o icone do proprio exe (guardiao); escudo do sistema como fallback
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
            Text = "Resource Pins - guardiao de camera/mic/tela",
            Visible = true,
            ContextMenuStrip = menu,
        };
    }

    private void AnchorToCorner()
    {
        // Canto superior direito da tela primaria, com uma folguinha
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
