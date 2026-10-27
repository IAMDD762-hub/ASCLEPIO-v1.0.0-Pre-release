namespace Asclepio.Escritorio;

// Edita aquí la identidad visual del prototipo.
internal static class Tema
{
    public static readonly Color Fondo = Color.FromArgb(242, 246, 249);
    public static readonly Color Tinta = Color.FromArgb(23, 43, 64);
    public static readonly Color Suave = Color.FromArgb(94, 112, 128);
    public static readonly Color Teal = Color.FromArgb(0, 120, 114);
    public static readonly Color Borde = Color.FromArgb(218, 228, 235);
    public static Label Texto(string texto, float tam = 10, bool negrita = false) => new()
    {
        Text = texto, ForeColor = Tinta, BackColor = Color.Transparent,
        Font = new Font("Segoe UI", tam, negrita ? FontStyle.Bold : FontStyle.Regular),
        Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
        AutoEllipsis = true, Margin = new Padding(0)
    };
    public static Button Boton(string texto, Action accion, bool principal = false)
    {
        var b = new Button
        {
            Text = texto, AutoSize = false, Height = 34, Width = 258,
            FlatStyle = FlatStyle.Flat, BackColor = principal ? Teal : Color.White,
            ForeColor = principal ? Color.White : Tinta, Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9, FontStyle.Bold), Margin = new Padding(0, 3, 0, 5)
        };
        b.FlatAppearance.BorderColor = principal ? Teal : Borde;
        b.Click += (_, _) => accion();
        return b;
    }
    public static TableLayoutPanel Tabla(int columnas, int filas) => new()
    {
        ColumnCount = columnas, RowCount = filas, Dock = DockStyle.Fill,
        BackColor = Color.Transparent, Margin = new Padding(0), Padding = new Padding(0)
    };
}
