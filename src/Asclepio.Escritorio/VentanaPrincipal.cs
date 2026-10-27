using Asclepio.Core.Domain;
using Asclepio.Core.Engine;

namespace Asclepio.Escritorio;

// La pantalla se construye con código. Usa F7 para editarla, no el diseñador de formularios.
public sealed class VentanaPrincipal : Form
{
    private SesionDemo _sesion = new();
    private readonly Label _reloj = Tema.Texto("", 20, true);
    private readonly Label _modo = Tema.Texto("", 15, true);
    private readonly Label _esperando = Tema.Texto("", 15, true);
    private readonly Label _ocupados = Tema.Texto("", 15, true);
    private readonly Label _atendidos = Tema.Texto("", 15, true);
    private readonly Label _guia = Tema.Texto("", 10);
    private readonly Label _paso = Tema.Texto("", 12, true);
    private readonly Label _alerta = Tema.Texto("", 9);
    private readonly TextBox _nombre = new() { PlaceholderText = "Nombre ficticio", Width = 258 };
    private readonly ComboBox _gravedad = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 258 };
    private readonly ComboBox _motivo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 258 };
    private readonly NumericUpDown _afluencia = new()
    {
        Minimum = 0,
        Maximum = 100,
        DecimalPlaces = 0,
        Increment = 1,
        Value = 50,
        Width = 258
    };

    private readonly NumericUpDown _personal = new()
    {
        Minimum = 0,
        Maximum = 100,
        DecimalPlaces = 0,
        Increment = 1,
        Value = 100,
        Width = 258
    };
    private readonly DataGridView _cola = new();
    private readonly TextBox _bitacora = new();
    private readonly Dictionary<string, TarjetaMedico> _tarjetas = new();
    private Button _siguiente = null!;
    private Button _cancelar = null!;

    public VentanaPrincipal()
    {
        Text = "Asclepio · Prototipo del motor de consulta";
        Font = new Font("Segoe UI", 10);
        BackColor = Tema.Fondo;
        ForeColor = Tema.Tinta;
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimumSize = new Size(1180, 800);
        ClientSize = new Size(1380, 900);
        StartPosition = FormStartPosition.CenterScreen;
        ConstruirPantalla();
        Actualizar();
    }

    private void ConstruirPantalla()
    {
        // Una sola cuadrícula mantiene alineadas todas las tarjetas.
        var raiz = Tema.Tabla(4, 6);
        raiz.Padding = new Padding(15, 10, 15, 8);

        for (int i = 0; i < 4; i++)
            raiz.ColumnStyles.Add(new(SizeType.Percent, 25));

        raiz.RowStyles.Add(new(SizeType.Absolute, 65));  // Cabecera
        raiz.RowStyles.Add(new(SizeType.Absolute, 100)); // Indicadores
        raiz.RowStyles.Add(new(SizeType.Absolute, 250)); // Situación y médicos
        raiz.RowStyles.Add(new(SizeType.Percent, 52));   // Cola
        raiz.RowStyles.Add(new(SizeType.Percent, 48));   // Detalle
        raiz.RowStyles.Add(new(SizeType.Absolute, 28));  // Pie

        Controls.Add(raiz);

        // CABECERA: nombre a la izquierda y reloj a la derecha.
        var cabecera = Tema.Tabla(2, 1);
        cabecera.Margin = new Padding(5);
        cabecera.ColumnStyles.Add(new(SizeType.Percent, 100));
        cabecera.ColumnStyles.Add(new(SizeType.Absolute, 260));
        cabecera.RowStyles.Add(new(SizeType.Percent, 100));

        cabecera.Controls.Add(
            Tema.Texto("ASCLEPIO", 23, true), 0, 0);

        // El texto y la hora comparten una fila.
        var bloqueReloj = Tema.Tabla(2, 1);
        bloqueReloj.ColumnStyles.Add(new(SizeType.Percent, 100));
        bloqueReloj.ColumnStyles.Add(new(SizeType.Absolute, 100));
        bloqueReloj.RowStyles.Add(new(SizeType.Percent, 100));

        var simulado = Tema.Texto("RELOJ SIMULADO", 9, true);
        simulado.ForeColor = Tema.Teal;
        simulado.TextAlign = ContentAlignment.MiddleRight;
        simulado.Margin = new Padding(0, 0, 12, 0);

        _reloj.TextAlign = ContentAlignment.MiddleRight;

        bloqueReloj.Controls.Add(simulado, 0, 0);
        bloqueReloj.Controls.Add(_reloj, 1, 0);
        cabecera.Controls.Add(bloqueReloj, 1, 0);

        raiz.Controls.Add(cabecera, 0, 0);
        raiz.SetColumnSpan(cabecera, 4);

        // INDICADORES: una tarjeta por columna.
        var indicadores = new[]
        {
        ("MODO ACTIVO", _modo),
        ("EN ESPERA", _esperando),
        ("EN CONSULTA", _ocupados),
        ("CONSULTAS TERMINADAS", _atendidos)
    };

        for (int columna = 0; columna < indicadores.Length; columna++)
        {
            var (titulo, valor) = indicadores[columna];

            var caja = Tema.Tabla(1, 2);
            caja.BackColor = Color.White;
            caja.Padding = new Padding(14, 10, 14, 10);
            caja.Margin = new Padding(5);
            caja.RowStyles.Add(new(SizeType.Absolute, 23));
            caja.RowStyles.Add(new(SizeType.Percent, 100));

            var etiqueta = Tema.Texto(titulo, 8, true);
            etiqueta.ForeColor = Tema.Suave;

            caja.Controls.Add(etiqueta, 0, 0);
            caja.Controls.Add(valor, 0, 1);

            raiz.Controls.Add(caja, columna, 1);
        }

        // SITUACIÓN: mismo tamaño exterior que las tarjetas médicas.
        raiz.Controls.Add(CrearSituacion(), 0, 2);

        // MÉDICOS: directamente en las columnas de la cuadrícula.
        _tarjetas.Clear();
        int columnaMedico = 1;

        foreach (var medico in _sesion.Medicos.ObtenerTodos())
        {
            var tarjeta = new TarjetaMedico(
                medico.Id,
                () => Manual(() => _sesion.Finalizar(medico.Id)),
                () => Manual(() => _sesion.QuitarBloque(medico.Id)));

            tarjeta.Margin = new Padding(5);
            _tarjetas.Add(medico.Id, tarjeta);

            raiz.Controls.Add(tarjeta, columnaMedico, 2);
            columnaMedico++;
        }

        // CONTROLES: debajo de Situación.
        var controles = CrearControles();
        controles.Margin = new Padding(5);
        raiz.Controls.Add(controles, 0, 3);
        raiz.SetRowSpan(controles, 2);

        // COLA: ocupa el ancho de las tres columnas médicas.
        var cola = CrearCola();
        cola.Margin = new Padding(5);
        raiz.Controls.Add(cola, 1, 3);
        raiz.SetColumnSpan(cola, 3);

        // DETALLE: alineado con la cola.
        var detalle = CrearDetalle();
        detalle.Margin = new Padding(5);
        raiz.Controls.Add(detalle, 1, 4);
        raiz.SetColumnSpan(detalle, 3);

    }

    private Control CrearSituacion()
    {
        var panel = Tema.Tabla(1, 4);
        panel.BackColor = Color.White;
        panel.Padding = new Padding(14, 10, 14, 10);
        panel.Margin = new Padding(5);

        panel.RowStyles.Add(new(SizeType.Absolute, 32));
        panel.RowStyles.Add(new(SizeType.Percent, 100));
        panel.RowStyles.Add(new(SizeType.Absolute, 40));
        panel.RowStyles.Add(new(SizeType.Absolute, 40));

        _paso.Dock = DockStyle.Fill;
        _paso.Margin = new Padding(0);
        _paso.TextAlign = ContentAlignment.MiddleLeft;

        _guia.Dock = DockStyle.Fill;
        _guia.Margin = new Padding(0, 4, 0, 6);
        _guia.TextAlign = ContentAlignment.TopLeft;

        _siguiente = Tema.Boton(
            "Siguiente paso →",
            () => Ejecutar(() => _sesion.Siguiente()),
            true);

        _siguiente.Dock = DockStyle.Fill;
        _siguiente.Margin = new Padding(0, 3, 0, 3);

        var reiniciar = Tema.Boton(
            "Reiniciar demostración", Reiniciar);

        reiniciar.Dock = DockStyle.Fill;
        reiniciar.Margin = new Padding(0, 3, 0, 3);

        panel.Controls.Add(_paso, 0, 0);
        panel.Controls.Add(_guia, 0, 1);
        panel.Controls.Add(_siguiente, 0, 2);
        panel.Controls.Add(reiniciar, 0, 3);

        return panel;
    }

    private Control CrearControles()
    {
        var p = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.White,
            Padding = new Padding(12),
            Margin = new Padding(0)
        };
        void Agregar(Control control, int alto = 0)
        {
            control.Dock = DockStyle.None;
            control.Width = 258;
            if (alto > 0) control.Height = alto;
            control.Margin = new Padding(0, 2, 0, 5);
            p.Controls.Add(control);
        }
        void Titulo(string t) => Agregar(Tema.Texto(t, 10, true), 27);

        Titulo("AGREGAR PACIENTE");
        Agregar(_nombre);
        _gravedad.Items.AddRange(new object[] { NivelGravedad.Amarillo, NivelGravedad.Verde, NivelGravedad.Azul });
        _gravedad.SelectedIndex = 1;
        Agregar(_gravedad);
        _motivo.Items.AddRange(new object[] { "Consulta larga", "Revisión general", "Receta de continuidad" });
        _motivo.SelectedIndex = 1;
        Agregar(_motivo);
        Agregar(Tema.Boton("Registrar llegada", () => Manual(() =>
        {
            _sesion.Agregar(_nombre.Text, (NivelGravedad)_gravedad.SelectedItem!, _motivo.Text);
            _nombre.Clear();
        }), true));
        Titulo("CONDICIONES SIMULADAS");
        Agregar(Tema.Texto("Afluencia (%)", 9), 22);
        Agregar(_afluencia);
        Agregar(Tema.Texto("Personal disponible (%)", 9), 22);
        Agregar(_personal);
        Agregar(Tema.Boton("Aplicar condiciones", () => Manual(() => _sesion.CambiarCarga((double)_afluencia.Value, (double)_personal.Value))));
        void AjustarAnchos()
        {
            int ancho = Math.Max(
                1,
                p.ClientSize.Width
                - p.Padding.Horizontal
                - SystemInformation.VerticalScrollBarWidth);

            foreach (Control control in p.Controls)
            {
                control.Width = Math.Max(
                    1, ancho - control.Margin.Horizontal);
            }
        }

        p.SizeChanged += (_, _) => AjustarAnchos();
        AjustarAnchos();
        return p;
    }

    private Control CrearMedicos()
    {
        var fila = Tema.Tabla(3, 1);
        foreach (var medico in _sesion.Medicos.ObtenerTodos())
        {
            fila.ColumnStyles.Add(new(SizeType.Percent, 100f / 3));
            var card = new TarjetaMedico(medico.Id,
                () => Manual(() => _sesion.Finalizar(medico.Id)),
                () => Manual(() => _sesion.QuitarBloque(medico.Id)));
            card.Margin = new Padding(0, 0, 10, 10);
            _tarjetas.Add(medico.Id, card);
            fila.Controls.Add(card);
        }
        return fila;
    }

    private Control CrearCola()
    {
        var panel = Tema.Tabla(1, 3);
        panel.BackColor = Color.White;
        panel.Padding = new Padding(10);
        panel.Margin = new Padding(0, 0, 10, 10);
        panel.RowStyles.Add(new(SizeType.Absolute, 30));
        panel.RowStyles.Add(new(SizeType.Percent, 100));
        panel.RowStyles.Add(new(SizeType.Absolute, 34));
        panel.Controls.Add(Tema.Texto("Cola de pacientes  ·  orden calculado por el motor", 12, true), 0, 0);
        _cola.Dock = DockStyle.Fill;
        _cola.ReadOnly = true;
        _cola.AllowUserToAddRows = false;
        _cola.AllowUserToDeleteRows = false;
        _cola.AllowUserToResizeRows = false;
        _cola.RowHeadersVisible = false;
        _cola.MultiSelect = false;
        _cola.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _cola.BackgroundColor = Color.White;
        _cola.BorderStyle = BorderStyle.None;
        _cola.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _cola.EnableHeadersVisualStyles = false;
        _cola.ColumnHeadersHeight = 32;
        _cola.ColumnHeadersDefaultCellStyle.BackColor = Tema.Fondo;
        _cola.ColumnHeadersDefaultCellStyle.ForeColor = Tema.Suave;
        _cola.DefaultCellStyle.SelectionBackColor = Color.FromArgb(218, 239, 236);
        _cola.DefaultCellStyle.SelectionForeColor = Tema.Tinta;
        _cola.RowTemplate.Height = 32;
        _cola.GridColor = Tema.Borde;
        _cola.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        foreach (var (nombre, peso) in new[] { ("Paciente", 90), ("Nivel", 65), ("Motivo", 130), ("Esperó", 55), ("Espera restante*", 100) })
        {
            var index = _cola.Columns.Add(nombre, nombre);
            _cola.Columns[index].FillWeight = peso;
            _cola.Columns[index].SortMode = DataGridViewColumnSortMode.NotSortable;
        }
        _cola.SelectionChanged += (_, _) => { if (_cancelar is not null) _cancelar.Enabled = _cola.SelectedRows.Count > 0; };
        panel.Controls.Add(_cola, 0, 1);
        var pie = Tema.Tabla(2, 1);
        pie.ColumnStyles.Add(new(SizeType.Percent, 100));
        pie.ColumnStyles.Add(new(SizeType.Absolute, 170));
        _cancelar = Tema.Boton("Retirar seleccionado", () =>
        {
            if (_cola.SelectedRows.Count == 0) return;
            string curp = (string)_cola.SelectedRows[0].Tag!;
            Manual(() => _sesion.Cancelar(curp));
        });
        _cancelar.Dock = DockStyle.Fill;
        pie.Controls.Add(_cancelar, 1, 0);
        panel.Controls.Add(pie, 0, 2);
        return panel;
    }

    private Control CrearDetalle()
    {
        var panel = Tema.Tabla(1, 1);
        panel.Margin = new Padding(5);
        panel.RowStyles.Add(new(SizeType.Percent, 100));
        var tabs = new TabControl { Dock = DockStyle.Fill };
        var registro = new TabPage("Decisiones del motor");
        _bitacora.Multiline = true;
        _bitacora.ReadOnly = true;
        _bitacora.Dock = DockStyle.Fill;
        _bitacora.ScrollBars = ScrollBars.Vertical;
        _bitacora.BackColor = Color.FromArgb(23, 43, 64);
        _bitacora.ForeColor = Color.FromArgb(218, 241, 236);
        _bitacora.Font = new Font("Consolas", 9);
        _bitacora.BorderStyle = BorderStyle.None;
        registro.Padding = new Padding(10);
        registro.BackColor = _bitacora.BackColor;
        registro.Controls.Add(_bitacora);
        tabs.TabPages.Add(registro);
        panel.Controls.Add(tabs, 0, 0);
        return panel;
    }

    private void Reiniciar()
    {
        if (_sesion.Nombres.Count > 0 && MessageBox.Show(this, "Se borrará únicamente la simulación actual. ¿Reiniciar?", "Reiniciar demo", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        _sesion = new();
        _nombre.Clear();
        Actualizar();
    }
    private void Manual(Action accion) => Ejecutar(accion);
    private void Ejecutar(Action accion)
    {
        try { accion(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "No se pudo completar la acción", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        Actualizar();
    }
    private void Actualizar()
    {
        _reloj.Text = _sesion.Reloj.Ahora.ToString("HH:mm");
        _modo.Text = EtiquetaModo(_sesion.Nucleo.Consulta.ModoActivo);
        _modo.ForeColor = Tema.Teal;
        _esperando.Text = $"{_sesion.Nucleo.Cola.Pacientes.Count} pacientes";
        _ocupados.Text = $"{_sesion.Medicos.ObtenerTodos().Count(m => m.Estado == EstadoMedico.Ocupado)} / 3 médicos";
        _atendidos.Text = _sesion.Atendidos.ToString();
        _paso.Text =
            $"Situación {_sesion.Paso}/{SesionDemo.TotalPasos}";

        _guia.Text = _sesion.Explicacion;

        _siguiente.Enabled =
            _sesion.Paso < SesionDemo.TotalPasos;
        _siguiente.Text = _sesion.Paso == SesionDemo.TotalPasos ? "Recorrido completado" : "Siguiente paso →";
        _afluencia.Value =
            Math.Round((decimal)_sesion.Medidas.Afluencia * 100m);

        _personal.Value =
            Math.Round((decimal)_sesion.Medidas.Personal * 100m);
        foreach (var m in _sesion.Medicos.ObtenerTodos()) _tarjetas[m.Id].Actualizar(m, _sesion);
        _cola.Rows.Clear();
        foreach (var p in _sesion.ColaOrdenada)
        {
            var espera = _sesion.Ultimo.Esperas.TryGetValue(p.Curp, out var rango)
                ? $"{rango.P50.TotalMinutes:0}–{rango.P80.TotalMinutes:0} min" : "Sin estimación";
            int indice = _cola.Rows.Add(_sesion.Nombre(p.Curp), p.Gravedad, p.Motivo,
                $"{(_sesion.Reloj.Ahora - p.HoraLlegada).TotalMinutes:0} min", espera);
            var row = _cola.Rows[indice]; row.Tag = p.Curp;
            row.Cells[0].ToolTipText = $"Puntaje calculado: {_sesion.Puntaje(p):0.0}";
            row.Cells[1].Style.BackColor = p.Gravedad switch
            {
                NivelGravedad.Amarillo => Color.FromArgb(255, 235, 161),
                NivelGravedad.Verde => Color.FromArgb(205, 239, 219),
                _ => Color.FromArgb(211, 230, 255)
            };
        }
        _cancelar.Enabled = _cola.SelectedRows.Count > 0;
        int sinEstimacion = _sesion.Nucleo.Cola.Pacientes.Count(p => !_sesion.Ultimo.Esperas.ContainsKey(p.Curp));
        _alerta.Text = _sesion.Ultimo.Alertas.Count > 0
            ? string.Join("  |  ", _sesion.Ultimo.Alertas.Select(a => a.Mensaje))
            : sinEstimacion > 0 ? $"{sinEstimacion} paciente(s) sin estimación de espera. Revisa ventanas y roles; no equivale a espera cero."
            : "Estado actualizado. ";
        _bitacora.Text = string.Join(Environment.NewLine, _sesion.Historial);
        _bitacora.SelectionStart = _bitacora.TextLength;
        _bitacora.ScrollToCaret();
    }

    private void InitializeComponent()
    {

    }

    private static string EtiquetaModo(ModoOperacion modo) => modo switch
    {
        ModoOperacion.M1Valle => "M1 · Valle",
        ModoOperacion.M2Base => "M2 · Base",
        ModoOperacion.M3Pico => "M3 · Pico",
        ModoOperacion.M4Austero => "M4 · Austero",
        ModoOperacion.M5Tension => "M5 · Tensión",
        _ => "M6 · Crítico"
    };
}

internal sealed class TarjetaMedico : TableLayoutPanel
{
    private readonly Label _nombre = Tema.Texto("", 13, true);
    private readonly Label _estado = Tema.Texto("", 9, true);
    private readonly Label _paciente = Tema.Texto("", 10, true);
    private readonly Label _detalle = Tema.Texto("", 9);
    private readonly Button _finalizar;
    private readonly Button _bloque;

    public TarjetaMedico(string id, Action finalizar, Action cancelarBloque)
    {
        Name = "tarjeta" + id;
        Dock = DockStyle.Fill; BackColor = Color.White;
        ColumnCount = 1; RowCount = 6; Padding = new Padding(12, 8, 12, 8);
        foreach (int h in new[] { 28, 23, 28, 51, 30, 25 }) RowStyles.Add(new(SizeType.Absolute, h));
        Controls.Add(_nombre, 0, 0); Controls.Add(_estado, 0, 1);
        Controls.Add(_paciente, 0, 2); Controls.Add(_detalle, 0, 3);
        _finalizar = Tema.Boton("Finalizar consulta", finalizar); _finalizar.Dock = DockStyle.Fill;
        _finalizar.Margin = new Padding(0, 0, 0, 2); Controls.Add(_finalizar, 0, 4);
        _bloque = Tema.Boton("Cancelar bloque fijo", cancelarBloque); _bloque.Dock = DockStyle.Fill;
        _bloque.Font = new Font("Segoe UI", 8); _bloque.Margin = new Padding(0, 0, 0, 0);
        Controls.Add(_bloque, 0, 5);
    }
    public void Actualizar(Medico m, SesionDemo s)
    {
        _nombre.Text = m.Nombre + (m.EsEspecialista ? " · Esp." : " · Gral.");
        bool ocupado = m.Estado == EstadoMedico.Ocupado;
        bool habilitado = !m.EsEspecialista || s.Nucleo.Consulta.ModoActivo is not (ModoOperacion.M1Valle or ModoOperacion.M2Base);
        _estado.Text = ocupado ? "EN CONSULTA" : habilitado ? "DISPONIBLE" : "ESPECIALISTA · NO HABILITADO";
        _estado.ForeColor = ocupado ? Color.FromArgb(162, 101, 12) : habilitado ? Tema.Teal : Tema.Suave;
        _paciente.Text = ocupado ? s.Nombre(m.PacienteActual) : "Sin paciente asignado";
        string bloque = m.ProximoBloqueFijo is { } b ? $"Bloque fijo {b:HH:mm}" + (b <= s.Reloj.Ahora ? " · revisar agenda" : "") : "Sin bloque fijo próximo";
        _detalle.Text = (ocupado ? $"Libre estimado {m.LibreEstimadoA:HH:mm}" : "Ventana: " + (m.ProximoBloqueFijo is { } t ? $"{Math.Max(0, (t - s.Reloj.Ahora).TotalMinutes):0} min" : "sin límite conocido")) + Environment.NewLine + bloque;
        _finalizar.Enabled = ocupado;
        _bloque.Visible = m.ProximoBloqueFijo.HasValue;
    }
}
