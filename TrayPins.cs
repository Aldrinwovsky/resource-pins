using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using WinForms = System.Windows.Forms;

namespace ResourcePins;

/// <summary>
/// Um icone na area de notificacao (barra de tarefas) por recurso.
/// Fica visivel somente enquanto o recurso esta em uso ativo.
/// </summary>
public sealed class TrayPins : IDisposable
{
    private readonly Dictionary<string, WinForms.NotifyIcon> _icons = new();
    private readonly List<IntPtr> _handles = new();

    public TrayPins(IEnumerable<(string Key, string Label, char Glyph, Color Color)> defs,
                    WinForms.ContextMenuStrip menu)
    {
        foreach (var d in defs)
        {
            _icons[d.Key] = new WinForms.NotifyIcon
            {
                Icon = BuildIcon(d.Glyph, d.Color),
                Text = Trim(d.Label),
                Visible = false,
                ContextMenuStrip = menu,
            };
        }
    }

    /// <summary>Acende (mostra) ou apaga (esconde) o icone do recurso.</summary>
    public void Update(string key, bool active, string tooltip)
    {
        if (!_icons.TryGetValue(key, out var icon)) return;
        var text = Trim(tooltip);
        if (icon.Text != text) icon.Text = text;
        if (icon.Visible != active) icon.Visible = active;
    }

    // NotifyIcon.Text estoura se passar de 63 caracteres
    private static string Trim(string s) =>
        s.Length <= 63 ? s : s[..60] + "...";

    /// <summary>Desenha o pin (circulo colorido + glifo) como icone de 32px.</summary>
    private Icon BuildIcon(char glyph, Color color)
    {
        var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);

            using var fill = new SolidBrush(color);
            g.FillEllipse(fill, 1, 1, 30, 30);
            using var edge = new Pen(Color.FromArgb(190, 255, 255, 255), 2);
            g.DrawEllipse(edge, 1, 1, 29, 29);

            using var font = new Font("Segoe MDL2 Assets", 15, GraphicsUnit.Pixel);
            using var fmt = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            };
            g.DrawString(glyph.ToString(), font, Brushes.White,
                new RectangleF(0, 0, 32, 32), fmt);
        }

        var handle = bmp.GetHicon();
        _handles.Add(handle);
        bmp.Dispose();
        // Clona para nao depender do handle na hora de desenhar
        using var temp = Icon.FromHandle(handle);
        return (Icon)temp.Clone();
    }

    public void Dispose()
    {
        foreach (var icon in _icons.Values)
        {
            icon.Visible = false;
            icon.Icon?.Dispose();
            icon.Dispose();
        }
        _icons.Clear();
        foreach (var h in _handles) DestroyIcon(h);
        _handles.Clear();
    }

    [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr hIcon);
}
