using System.Text.Json;
namespace Asclepio;

// Contrato independiente de Windows Forms, listo para ser utilizado por una API.
public sealed class PaqueteCarnet
{
    public string Esquema { get; set; } = "asclepio.carnet";
    public int Version { get; set; } = 1;
    public DateTimeOffset ExportadoUtc { get; set; } = DateTimeOffset.UtcNow;
    public CarneCURP Paciente { get; set; } = new();
}
public sealed class ArchivoPacientes
{
    public int Version { get; set; } = 1;
    public List<CarneCURP> Pacientes { get; set; } = new();
}
public static class Intercambio
{
    public static readonly JsonSerializerOptions Opciones = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    public static CarneCURP Copiar(CarneCURP p) => JsonSerializer.Deserialize<CarneCURP>(JsonSerializer.Serialize(p, Opciones), Opciones)!;
    public static void Exportar(string ruta, CarneCURP p)
    {
        Validacion.Carnet(p);
        ArchivoAtomico.Escribir(ruta, JsonSerializer.Serialize(new PaqueteCarnet { Paciente = p }, Opciones));
    }
    public static CarneCURP Importar(string ruta)
    {
        string json = LeerLimitado(ruta);
        var paquete = JsonSerializer.Deserialize<PaqueteCarnet>(json, Opciones) ?? throw new ArgumentException("Archivo vacío.");
        // Exige el discriminador en el archivo, no sólo el valor por defecto de la clase.
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("esquema", out var esquema) || esquema.GetString() != "asclepio.carnet" || !doc.RootElement.TryGetProperty("version", out var version) || version.GetInt32() != 1 || !doc.RootElement.TryGetProperty("paciente", out _))
            throw new ArgumentException("El archivo no es un carnet Asclepio versión 1.");
        Validacion.Carnet(paquete.Paciente);
        return paquete.Paciente;
    }
    public static string LeerLimitado(string ruta)
    {
        if (new FileInfo(ruta).Length > 5 * 1024 * 1024) throw new ArgumentException("El archivo supera el límite de 5 MB.");
        return File.ReadAllText(ruta);
    }
}
public static class ArchivoAtomico
{
    public static void Escribir(string ruta, string contenido)
    {
        ruta = Path.GetFullPath(ruta);
        Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
        string temporal = ruta + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporal, contenido);
            if (File.Exists(ruta)) File.Copy(ruta, ruta + ".bak", true);
            File.Move(temporal, ruta, true);
        }
        finally { if (File.Exists(temporal)) File.Delete(temporal); }
    }
}
public sealed class RepositorioCarnets
{
    public string Ruta { get; }
    private List<CarneCURP> _pacientes = new();
    public IReadOnlyList<CarneCURP> Pacientes => _pacientes.Select(Intercambio.Copiar).ToArray();
    public RepositorioCarnets(string ruta)
    {
        Ruta = ruta;
        if (!File.Exists(ruta)) return;
        string json = Intercambio.LeerLimitado(ruta);
        using var documento = JsonDocument.Parse(json);
        if (!documento.RootElement.TryGetProperty("version", out var version) || version.GetInt32() != 1 || !documento.RootElement.TryGetProperty("pacientes", out _)) throw new ArgumentException("El archivo local no tiene el formato esperado.");
        var archivo = JsonSerializer.Deserialize<ArchivoPacientes>(json, Intercambio.Opciones) ?? throw new ArgumentException("Archivo local vacío.");
        if (archivo.Version != 1 || archivo.Pacientes is null) throw new ArgumentException("Versión de archivo local no compatible.");
        foreach (var p in archivo.Pacientes) Validacion.Carnet(p);
        if (archivo.Pacientes.Select(p => p.Curp).Distinct().Count() != archivo.Pacientes.Count) throw new ArgumentException("El archivo local contiene CURP duplicadas.");
        _pacientes = archivo.Pacientes;
    }
    public void Guardar(CarneCURP paciente, string? curpOriginal)
    {
        var copia = Intercambio.Copiar(paciente);
        Validacion.Carnet(copia);
        if (_pacientes.Any(p => p.Curp == copia.Curp && p.Curp != curpOriginal)) throw new ArgumentException("Ya existe un carnet con esa CURP. Selecciónalo en la lista para editarlo.");
        var nuevos = _pacientes.Where(p => p.Curp != curpOriginal).Select(Intercambio.Copiar).ToList();
        nuevos.Add(copia);
        var json = JsonSerializer.Serialize(new ArchivoPacientes { Pacientes = nuevos }, Intercambio.Opciones);
        if (System.Text.Encoding.UTF8.GetByteCount(json) > 5 * 1024 * 1024) throw new ArgumentException("Se alcanzó el límite de almacenamiento del prototipo (5 MB).");
        ArchivoAtomico.Escribir(Ruta, json);
        _pacientes = nuevos;
    }
}
