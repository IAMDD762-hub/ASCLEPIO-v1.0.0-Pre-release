using Asclepio;
namespace Asclepio.Carnet.Escritorio;
internal sealed class RegistroDialogo : Form
{
    public CitaConsulta? Consulta { get; private set; }
    public RegistroUrgencia? Urgencia { get; private set; }
    public RegistroDialogo(bool urgencia)
    {
        Text=urgencia?"Registrar urgencia":"Registrar consulta / cita";Font=new Font("Segoe UI",10);BackColor=Color.White;ForeColor=Tema.Tinta;AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(560,410);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;
        var t=Tema.Tabla(1,8);t.Padding=new Padding(22);Controls.Add(t);foreach(int alto in new[]{30,30,28,32,28,130,20,50})t.RowStyles.Add(new(SizeType.Absolute,alto));
        var fecha=new DateTimePicker{Dock=DockStyle.Fill,Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy HH:mm",MinDate=new DateTime(1900,1,1),Value=DateTime.Now};
        var medico=new TextBox{Dock=DockStyle.Fill,MaxLength=150};var consultorio=new TextBox{Dock=DockStyle.Top,MaxLength=80};
        var nivel=new ComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList};nivel.Items.AddRange(new object[]{"1 · Máxima prioridad","2","3","4","5 · Menor prioridad"});nivel.SelectedIndex=2;
        var sintomas=new TextBox{Dock=DockStyle.Fill,Multiline=true,MaxLength=2000,ScrollBars=ScrollBars.Vertical};
        t.Controls.Add(Tema.Texto(urgencia?"Fecha y hora de ingreso":"Fecha y hora",10,true),0,0);t.Controls.Add(fecha,0,1);
        t.Controls.Add(Tema.Texto(urgencia?"Nivel de triage registrado":"Médico asignado"),0,2);t.Controls.Add(urgencia?nivel:medico,0,3);
        t.Controls.Add(Tema.Texto(urgencia?"Síntomas registrados":"Consultorio"),0,4);t.Controls.Add(urgencia?sintomas:consultorio,0,5);
        var b=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};var aceptar=Tema.Boton("Agregar",()=>{try{if(urgencia){var u=new RegistroUrgencia{FechaIngreso=fecha.Value,NivelTriage=nivel.SelectedIndex+1,Sintomas=sintomas.Text.Trim()};Validacion.Urgencia(u);Urgencia=u;}else{var c=new CitaConsulta{Fecha=fecha.Value,MedicoAsignado=medico.Text.Trim(),Consultorio=consultorio.Text.Trim()};Validacion.Consulta(c);Consulta=c;}DialogResult=DialogResult.OK;}catch(Exception ex){MessageBox.Show(this,ex.Message,"Revisa el registro");}},true);
        var cancelar=Tema.Boton("Cancelar",()=>DialogResult=DialogResult.Cancel);b.Controls.Add(aceptar);b.Controls.Add(cancelar);t.Controls.Add(b,0,7);CancelButton=cancelar;
    }
}
