using Asclepio;
namespace Asclepio.Carnet.Escritorio;
internal static class Program
{
    [STAThread] static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var instancia = new Mutex(true, "Asclepio.Carnet.Prototipo.v1", out bool primera);
        if (!primera) { MessageBox.Show("El carnet ya está abierto. Revisa la barra de tareas.", "Asclepio"); return; }
        var ruta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Asclepio", "Carnet", "pacientes.json");
        try { Application.Run(new VentanaCarnet(new RepositorioCarnets(ruta))); }
        catch (Exception ex) { MessageBox.Show("No se pudo abrir el carnet. No se reemplazaron los datos existentes.\n\n" + ex.Message + "\n\nDatos: " + ruta, "Asclepio", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
