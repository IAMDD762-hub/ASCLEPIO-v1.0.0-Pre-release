using Asclepio;
namespace Asclepio.Carnet.Escritorio;

// Interfaz por código: F7. Los modelos y el guardado están en Carnet.Core.
public sealed class VentanaCarnet : Form
{
    private readonly RepositorioCarnets _repo;
    private CarneCURP _actual = new();
    private string? _original;
    private bool _cargando, _sucio, _listaCargando;
    private readonly TextBox _buscar = new() { PlaceholderText = "Buscar nombre o CURP", Dock = DockStyle.Fill };
    private readonly ListBox _lista = new() { Dock=DockStyle.Fill, BorderStyle=BorderStyle.None, IntegralHeight=false, ItemHeight=56, DrawMode=DrawMode.OwnerDrawFixed };
    private readonly Dictionary<string,TextBox> _campos = new();
    private readonly DateTimePicker _nacimiento = new() { Format=DateTimePickerFormat.Custom, CustomFormat="dd/MM/yyyy", MinDate=new DateTime(1900,1,1), MaxDate=DateTime.Today, Dock=DockStyle.Fill };
    private readonly ComboBox _sangre = new() { DropDownStyle=ComboBoxStyle.DropDownList, Dock=DockStyle.Fill };
    private readonly Label _titulo = Tema.Texto("Nuevo carnet",22,true), _subtitulo = Tema.Texto("",10), _estado = Tema.Texto("",9), _edad = Tema.Texto("",12,true);
    private readonly Label _nConsultas = Tema.Texto("0",18,true), _nUrgencias = Tema.Texto("0",18,true);
    private readonly DataGridView _consultas = Tema.Lista("Fecha", "Médico asignado", "Consultorio");
    private readonly DataGridView _urgencias = Tema.Lista("Ingreso", "Triage", "Síntomas");

    public VentanaCarnet(RepositorioCarnets repo)
    {
        _repo=repo; Text="Asclepio · Carnet unificado"; Font=new Font("Segoe UI",10); BackColor=Tema.Fondo; ForeColor=Tema.Tinta;
        AutoScaleMode=AutoScaleMode.Dpi; ClientSize=new Size(1220,850); MinimumSize=new Size(1080,760); StartPosition=FormStartPosition.CenterScreen;
        Construir(); ActualizarLista(); Cargar(new CarneCURP(),null);
        FormClosing += (_, e) => { if (!ResolverCambios()) e.Cancel=true; };
    }
    private void Construir()
    {
        var raiz=Tema.Tabla(1,3); raiz.Padding=new Padding(20,12,20,10);
        raiz.RowStyles.Add(new(SizeType.Absolute,74)); raiz.RowStyles.Add(new(SizeType.Percent,100)); raiz.RowStyles.Add(new(SizeType.Absolute,30)); Controls.Add(raiz);
        var header=Tema.Tabla(2,1); header.ColumnStyles.Add(new(SizeType.Percent,55)); header.ColumnStyles.Add(new(SizeType.Percent,45));
        header.Controls.Add(Tema.Texto("ASCLEPIO  /  Carnet",23,true),0,0);
        var leyenda=Tema.Texto("PROTOTIPO · DATOS DE PRUEBA",9,true); leyenda.ForeColor=Tema.Teal; leyenda.TextAlign=ContentAlignment.MiddleRight; header.Controls.Add(leyenda,1,0); raiz.Controls.Add(header,0,0);
        var cuerpo=Tema.Tabla(2,1); cuerpo.ColumnStyles.Add(new(SizeType.Absolute,275)); cuerpo.ColumnStyles.Add(new(SizeType.Percent,100)); raiz.Controls.Add(cuerpo,0,1);
        var lateral=Tema.Tabla(1,6); lateral.BackColor=Color.White; lateral.Padding=new Padding(14); lateral.Margin=new Padding(0,0,12,0);
        foreach(int h in new[]{34,42,48}) lateral.RowStyles.Add(new(SizeType.Absolute,h)); lateral.RowStyles.Add(new(SizeType.Percent,100)); lateral.RowStyles.Add(new(SizeType.Absolute,46)); lateral.RowStyles.Add(new(SizeType.Absolute,46));
        lateral.Controls.Add(Tema.Texto("PACIENTES",10,true),0,0); lateral.Controls.Add(_buscar,0,1);
        var nuevo=Tema.Boton("+ Nuevo carnet",()=>CambiarANuevo(false),true); nuevo.Dock=DockStyle.Fill; lateral.Controls.Add(nuevo,0,2);
        lateral.Controls.Add(_lista,0,3);
        var importar=Tema.Boton("Importar carnet",Importar); importar.Dock=DockStyle.Fill; lateral.Controls.Add(importar,0,4);
        var demo=Tema.Boton("Cargar ejemplo ficticio",()=>CambiarANuevo(true)); demo.Dock=DockStyle.Fill; lateral.Controls.Add(demo,0,5); cuerpo.Controls.Add(lateral,0,0);
        _buscar.TextChanged+=(_,_)=>ActualizarLista();
        _lista.DrawItem+=DibujarPaciente; _lista.SelectedIndexChanged+=(_,_)=>SeleccionarPaciente();
        var derecho=Tema.Tabla(1,4); derecho.RowStyles.Add(new(SizeType.Absolute,98)); derecho.RowStyles.Add(new(SizeType.Absolute,90)); derecho.RowStyles.Add(new(SizeType.Percent,100)); derecho.RowStyles.Add(new(SizeType.Absolute,54)); cuerpo.Controls.Add(derecho,1,0);
        var ficha=Tema.Tabla(1,2); ficha.BackColor=Color.White; ficha.Padding=new Padding(18,8,18,8); ficha.Margin=new Padding(0,0,0,10); ficha.RowStyles.Add(new(SizeType.Percent,60)); ficha.RowStyles.Add(new(SizeType.Percent,40)); ficha.Controls.Add(_titulo,0,0); ficha.Controls.Add(_subtitulo,0,1); derecho.Controls.Add(ficha,0,0);
        var metricas=Tema.Tabla(3,1); var valores=new[]{("EDAD",_edad),("CONSULTAS / CITAS",_nConsultas),("URGENCIAS",_nUrgencias)};
        for(int i=0;i<3;i++) { metricas.ColumnStyles.Add(new(SizeType.Percent,100f/3)); var t=Tema.Tabla(1,2); t.BackColor=Color.White; t.Padding=new Padding(14,6,14,6); t.Margin=new Padding(0,0,i==2?0:10,10); t.RowStyles.Add(new(SizeType.Absolute,22)); t.RowStyles.Add(new(SizeType.Percent,100)); var l=Tema.Texto(valores[i].Item1,8,true);l.ForeColor=Tema.Suave; t.Controls.Add(l,0,0);t.Controls.Add(valores[i].Item2,0,1);metricas.Controls.Add(t,i,0); } derecho.Controls.Add(metricas,0,1);
        var tabs=new TabControl{Dock=DockStyle.Fill,Padding=new Point(18,8)};
        tabs.TabPages.Add(Datos()); tabs.TabPages.Add(Historial(false)); tabs.TabPages.Add(Historial(true)); derecho.Controls.Add(tabs,0,2);
        var acciones=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(0,10,0,0),WrapContents=false};
        acciones.Controls.Add(Tema.Boton("Guardar carnet",()=>Guardar(),true)); acciones.Controls.Add(Tema.Boton("Exportar carnet",Exportar)); acciones.Controls.Add(Tema.Boton("Abrir carpeta de datos",()=>Ejecutar(()=>{Directory.CreateDirectory(Path.GetDirectoryName(_repo.Ruta)!);System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe",Path.GetDirectoryName(_repo.Ruta)!){UseShellExecute=true});}))); derecho.Controls.Add(acciones,0,3);
        _estado.ForeColor=Tema.Suave; raiz.Controls.Add(_estado,0,2);
    }
    private TabPage Datos()
    {
        var tab=new TabPage("Datos del paciente"){BackColor=Color.White,Padding=new Padding(14),AutoScroll=true};
        var tabla=Tema.Tabla(2,7); tabla.Dock=DockStyle.Top; tabla.Height=476; tabla.ColumnStyles.Add(new(SizeType.Percent,50)); tabla.ColumnStyles.Add(new(SizeType.Percent,50));
        for(int i=0;i<7;i++)tabla.RowStyles.Add(new(SizeType.Absolute,68));
        void Campo(string clave,string titulo,int col,int fila,int max=150,bool lectura=false){var t=new TextBox{Dock=DockStyle.Fill,MaxLength=max,ReadOnly=lectura}; _campos.Add(clave,t); t.TextChanged+=(_,_)=>Cambio(); Poner(t,titulo,col,fila);}
        void Poner(Control c,string titulo,int col,int fila){var p=Tema.Tabla(1,2);p.Margin=new Padding(0,0,col==0?16:0,8);p.RowStyles.Add(new(SizeType.Absolute,25));p.RowStyles.Add(new(SizeType.Percent,100));var l=Tema.Texto(titulo,9);l.ForeColor=Tema.Suave;p.Controls.Add(l,0,0);p.Controls.Add(c,0,1);tabla.Controls.Add(p,col,fila);}
        Campo("nombre","Nombre completo *",0,0);Campo("curp","CURP * · revisión de formato",1,0,18);
        Poner(_nacimiento,"Fecha de nacimiento *",0,1); _nacimiento.ValueChanged+=(_,_)=>{Cambio();MostrarEdad();};
        _sangre.Items.AddRange(Validacion.TiposSangre); Poner(_sangre,"Tipo de sangre declarado",1,1);_sangre.SelectedIndexChanged+=(_,_)=>Cambio();
        Campo("telefono","Teléfono",0,2,25);Campo("folio","Folio del carnet",1,2,80,true);
        Campo("calle","Calle y número",0,3,200);Campo("colonia","Colonia",1,3,200);
        Campo("municipio","Alcaldía / municipio",0,4,200);Campo("cp","Código postal",1,4,5);
        var nota=Tema.Texto("* Obligatorios. La CURP y los datos capturados no verifican por sí solos la identidad del paciente.",9);nota.ForeColor=Tema.Suave;tabla.Controls.Add(nota,0,5);tabla.SetColumnSpan(nota,2);
        var privacidad=Tema.Texto("Demostración local: utiliza datos ficticios. El archivo local y las exportaciones no están cifrados.",9);privacidad.ForeColor=Tema.Suave;tabla.Controls.Add(privacidad,0,6);tabla.SetColumnSpan(privacidad,2);
        tab.Controls.Add(tabla);return tab;
    }
    private TabPage Historial(bool urgencia)
    {
        var tab=new TabPage(urgencia?"Urgencias":"Consultas / citas"){BackColor=Color.White,Padding=new Padding(14)};
        var t=Tema.Tabla(1,3);t.RowStyles.Add(new(SizeType.Absolute,48));t.RowStyles.Add(new(SizeType.Percent,100));t.RowStyles.Add(new(SizeType.Absolute,48));
        t.Controls.Add(Tema.Texto(urgencia?"Nivel de triage registrado por personal autorizado; no se calcula automáticamente.":"Registros de consultas y citas. Agregar una cita no reserva recursos en el motor.",9),0,0);
        t.Controls.Add(urgencia?_urgencias:_consultas,0,1);
        var botones=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(0,8,0,0),WrapContents=false};
        botones.Controls.Add(Tema.Boton(urgencia?"+ Registrar urgencia":"+ Registrar consulta",()=>AgregarRegistro(urgencia),true));
        botones.Controls.Add(Tema.Boton("Quitar seleccionado",()=>QuitarRegistro(urgencia)));t.Controls.Add(botones,0,2);tab.Controls.Add(t);return tab;
    }
    private void DibujarPaciente(object? sender, DrawItemEventArgs e)
    {
        if(e.Index<0)return; var p=(CarneCURP)_lista.Items[e.Index]; bool seleccionado=(e.State&DrawItemState.Selected)!=0;
        using var fondo=new SolidBrush(seleccionado?Color.FromArgb(218,239,236):Color.White); e.Graphics.FillRectangle(fondo,e.Bounds);
        using var negrita=new Font(Font,FontStyle.Bold);var r=new Rectangle(e.Bounds.X+8,e.Bounds.Y+7,e.Bounds.Width-16,22);
        TextRenderer.DrawText(e.Graphics,p.NombreCompleto,negrita,r,Tema.Tinta,TextFormatFlags.EndEllipsis);
        r.Y+=23;TextRenderer.DrawText(e.Graphics,p.Curp,Font,r,Tema.Suave,TextFormatFlags.EndEllipsis);
    }
    private void ActualizarLista()
    {
        _listaCargando=true;
        try{_lista.Items.Clear();string q=_buscar.Text.Trim();foreach(var p in _repo.Pacientes.Where(p=>p.NombreCompleto.Contains(q,StringComparison.OrdinalIgnoreCase)||p.Curp.Contains(q,StringComparison.OrdinalIgnoreCase)).OrderBy(p=>p.NombreCompleto))_lista.Items.Add(p);RestaurarSeleccion();}
        finally{_listaCargando=false;}
    }
    private void RestaurarSeleccion(){_lista.SelectedIndex=-1;for(int i=0;i<_lista.Items.Count;i++)if(((CarneCURP)_lista.Items[i]).Curp==_original){_lista.SelectedIndex=i;break;}}
    private void SeleccionarPaciente()
    {
        if(_listaCargando||_lista.SelectedItem is not CarneCURP p||p.Curp==_original)return;
        string destino=p.Curp;
        if(!ResolverCambios()){_listaCargando=true;RestaurarSeleccion();_listaCargando=false;return;}
        var elegido=_repo.Pacientes.FirstOrDefault(x=>x.Curp==destino);
        if(elegido is not null)Cargar(elegido,elegido.Curp);
    }
    private void Cambio(){if(_cargando)return;_sucio=true;_estado.Text="Cambios sin guardar";}
    private void MostrarEdad(){var fecha=DateOnly.FromDateTime(_nacimiento.Value); _edad.Text=new CarneCURP{FechaNacimiento=fecha}.CalcularEdad()+" años";}
    private void Cargar(CarneCURP p,string? original)
    {
        _cargando=true;_actual=Intercambio.Copiar(p);_original=original;
        _campos["nombre"].Text=p.NombreCompleto;_campos["curp"].Text=p.Curp;_campos["telefono"].Text=p.Numtel;_campos["folio"].Text=p.Folio;
        _campos["calle"].Text=p.Residencia.callenumero;_campos["colonia"].Text=p.Residencia.colonia;_campos["municipio"].Text=p.Residencia.alcaldiamunicipio;_campos["cp"].Text=p.Residencia.codpost;
        _nacimiento.Value=p.FechaNacimiento.ToDateTime(TimeOnly.MinValue);_sangre.SelectedItem=p.TipoSangre;
        _titulo.Text=string.IsNullOrWhiteSpace(p.NombreCompleto)?"Nuevo carnet":p.NombreCompleto;
        _subtitulo.Text=original is null?"Completa los datos y guarda para crear el carnet.":p.Curp+"   ·   "+p.Folio;
        MostrarEdad();ActualizarHistorial();_cargando=false;_sucio=false;_estado.Text=original is null?"Nuevo carnet · aún no guardado":"Carnet cargado · guardado en esta computadora";
        _listaCargando=true;RestaurarSeleccion();_listaCargando=false;
    }
    private CarneCURP Leer()
    {
        var p=Intercambio.Copiar(_actual);p.NombreCompleto=_campos["nombre"].Text.Trim();p.Curp=_campos["curp"].Text.Trim().ToUpperInvariant();p.Numtel=_campos["telefono"].Text.Trim();p.FechaNacimiento=DateOnly.FromDateTime(_nacimiento.Value);p.TipoSangre=_sangre.Text;
        p.Residencia=new Direccion{callenumero=_campos["calle"].Text.Trim(),colonia=_campos["colonia"].Text.Trim(),alcaldiamunicipio=_campos["municipio"].Text.Trim(),codpost=_campos["cp"].Text.Trim()};return p;
    }
    private bool Guardar()
    {
        try{var p=Leer();_repo.Guardar(p,_original);Cargar(p,p.Curp);ActualizarLista();_estado.Text="Carnet guardado · "+DateTime.Now.ToString("HH:mm:ss");return true;}
        catch(Exception ex){MessageBox.Show(this,ex.Message,"Revisa los datos",MessageBoxButtons.OK,MessageBoxIcon.Information);return false;}
    }
    private bool ResolverCambios()
    {
        if(!_sucio)return true;var respuesta=MessageBox.Show(this,"Hay cambios sin guardar. ¿Quieres guardarlos antes de continuar?","Asclepio",MessageBoxButtons.YesNoCancel,MessageBoxIcon.Question);
        return respuesta==DialogResult.No||(respuesta==DialogResult.Yes&&Guardar());
    }
    private void CambiarANuevo(bool demo)
    {
        if(!ResolverCambios())return;var p=new CarneCURP();
        if(demo){p.Curp="PEAL000101HDFRRN09";p.NombreCompleto="Paciente de demostración";p.Numtel="+52 5500000000";p.Residencia=new Direccion{callenumero="Calle de ejemplo 123",colonia="Colonia de prueba",alcaldiamunicipio="Municipio de prueba",codpost="00000"};p.HistorialConsultas.Add(new(){Fecha=DateTime.Today.AddDays(1).AddHours(9),MedicoAsignado="Dra. Ruiz",Consultorio="01"});p.HistorialUrgencias.Add(new(){FechaIngreso=DateTime.Today.AddDays(-7).AddHours(12),NivelTriage=4,Sintomas="Registro ficticio para demostrar el historial."});}
        Cargar(p,null);if(demo)Cambio();
    }
    private void ActualizarHistorial()
    {
        _consultas.Rows.Clear();foreach(var c in _actual.HistorialConsultas.OrderByDescending(c=>c.Fecha)){int i=_consultas.Rows.Add(c.Fecha.ToString("dd/MM/yyyy HH:mm"),c.MedicoAsignado,c.Consultorio);_consultas.Rows[i].Tag=c.Id;}
        _urgencias.Rows.Clear();foreach(var u in _actual.HistorialUrgencias.OrderByDescending(u=>u.FechaIngreso)){int i=_urgencias.Rows.Add(u.FechaIngreso.ToString("dd/MM/yyyy HH:mm"),$"Nivel {u.NivelTriage}",u.Sintomas);_urgencias.Rows[i].Tag=u.Id;_urgencias.Rows[i].Cells[2].ToolTipText=u.Sintomas;}
        _nConsultas.Text=_actual.HistorialConsultas.Count.ToString();_nUrgencias.Text=_actual.HistorialUrgencias.Count.ToString();
    }
    private void AgregarRegistro(bool urgencia)
    {
        using var d=new RegistroDialogo(urgencia);if(d.ShowDialog(this)!=DialogResult.OK)return;
        if(urgencia)_actual.HistorialUrgencias.Add(d.Urgencia!);else _actual.HistorialConsultas.Add(d.Consulta!);ActualizarHistorial();Cambio();
    }
    private void QuitarRegistro(bool urgencia)
    {
        var grid=urgencia?_urgencias:_consultas;if(grid.SelectedRows.Count==0)return;
        if(MessageBox.Show(this,"¿Quitar este registro del carnet? El cambio se aplicará al guardar.","Quitar registro",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
        var id=(Guid)grid.SelectedRows[0].Tag!;if(urgencia)_actual.HistorialUrgencias.RemoveAll(x=>x.Id==id);else _actual.HistorialConsultas.RemoveAll(x=>x.Id==id);ActualizarHistorial();Cambio();
    }
    private void Exportar()
    {
        if(!Guardar())return;
        using var d=new SaveFileDialog{Filter="Carnet Asclepio (*.json)|*.json",FileName="Carnet-"+_actual.Folio+".json",Title="Exportar copia del carnet",OverwritePrompt=true};
        if(d.ShowDialog(this)!=DialogResult.OK)return;Ejecutar(()=>{Intercambio.Exportar(d.FileName,_actual);_estado.Text="Carnet exportado. La integración con otros módulos requiere un importador.";});
    }
    private void Importar()
    {
        if(!ResolverCambios())return;using var d=new OpenFileDialog{Filter="Carnet Asclepio (*.json)|*.json",Title="Importar carnet"};if(d.ShowDialog(this)!=DialogResult.OK)return;
        Ejecutar(()=>{var p=Intercambio.Importar(d.FileName);var existe=_repo.Pacientes.FirstOrDefault(x=>x.Curp==p.Curp);
            if(existe is not null&&MessageBox.Show(this,"Ya existe esta CURP. ¿Reemplazar sus datos e historial con el contenido del archivo?","Importar carnet",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
            _repo.Guardar(p,existe?.Curp);Cargar(p,p.Curp);ActualizarLista();_estado.Text="Carnet importado y guardado";});
    }
    private void Ejecutar(Action accion){try{accion();}catch(Exception ex){MessageBox.Show(this,ex.Message,"No se pudo completar",MessageBoxButtons.OK,MessageBoxIcon.Information);}}
}
