namespace Asclepio.Carnet.Escritorio;
internal static class Tema
{
    public static readonly Color Fondo = Color.FromArgb(242,246,249), Tinta = Color.FromArgb(23,43,64), Teal = Color.FromArgb(0,120,114), Suave = Color.FromArgb(94,112,128), Borde = Color.FromArgb(218,228,235);
    public static Label Texto(string texto, float tam = 10, bool fuerte = false) => new() { Text = texto, Font = new Font("Segoe UI",tam,fuerte ? FontStyle.Bold : FontStyle.Regular), ForeColor = Tinta, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0), AutoEllipsis = true };
    public static TableLayoutPanel Tabla(int c, int r) => new() { ColumnCount = c, RowCount = r, Dock = DockStyle.Fill, Margin = new Padding(0), Padding = new Padding(0) };
    public static Button Boton(string texto, Action accion, bool principal = false)
    {
        var b = new Button { Text = texto, AutoSize = true, MinimumSize = new Size(120,36), Height = 36, FlatStyle = FlatStyle.Flat, BackColor = principal ? Teal : Color.White, ForeColor = principal ? Color.White : Tinta, Font = new Font("Segoe UI",10,FontStyle.Bold), Padding = new Padding(10,0,10,0), Margin = new Padding(0,0,8,0), Cursor = Cursors.Hand };
        b.FlatAppearance.BorderColor = principal ? Teal : Borde; b.Click += (_,_) => accion(); return b;
    }
    public static DataGridView Lista(params string[] columnas)
    {
        var g = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, RowHeadersVisible = false, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, EnableHeadersVisualStyles = false, ColumnHeadersHeight = 36 };
        g.ColumnHeadersDefaultCellStyle.BackColor = Fondo; g.ColumnHeadersDefaultCellStyle.ForeColor = Suave;
        g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(218,239,236); g.DefaultCellStyle.SelectionForeColor = Tinta;
        g.RowTemplate.Height = 36; g.GridColor = Borde; g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        foreach(var c in columnas) { int i=g.Columns.Add(c,c); g.Columns[i].SortMode=DataGridViewColumnSortMode.NotSortable; }
        return g;
    }
}
