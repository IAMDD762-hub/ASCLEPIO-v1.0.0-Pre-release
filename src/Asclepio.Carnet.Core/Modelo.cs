using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
namespace Asclepio;

public sealed class CarneCURP
{
    public string Curp { get; set; } = "";
    public string NombreCompleto { get; set; } = "";
    public string TipoSangre { get; set; } = "No conocido";
    public DateOnly FechaNacimiento { get; set; } = new(2000, 1, 1);
    public Direccion Residencia { get; set; } = new();
    // Se calcula siempre: no queda desactualizada tras un cumpleaños.
    [JsonIgnore] public int Edad => CalcularEdad();
    // Texto conserva prefijos internacionales y ceros iniciales.
    public string Numtel { get; set; } = "";
    public string Folio { get; set; } = "CAR-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
    public List<CitaConsulta> HistorialConsultas { get; set; } = new();
    public List<RegistroUrgencia> HistorialUrgencias { get; set; } = new();
    public int CalcularEdad() => CalcularEdad(DateOnly.FromDateTime(DateTime.Today));
    public int CalcularEdad(DateOnly hoy)
    {
        if (FechaNacimiento > hoy) throw new ArgumentException("La fecha de nacimiento no puede ser futura.");
        int edad = hoy.Year - FechaNacimiento.Year;
        if (hoy < FechaNacimiento.AddYears(edad)) edad--;
        return edad;
    }
}
public sealed class Direccion
{
    public string callenumero { get; set; } = "";
    public string colonia { get; set; } = "";
    public string alcaldiamunicipio { get; set; } = "";
    public string codpost { get; set; } = "";
}
public sealed class CitaConsulta
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Fecha { get; set; } = DateTime.Now;
    public string MedicoAsignado { get; set; } = "";
    public string Consultorio { get; set; } = "";
}
public sealed class RegistroUrgencia
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime FechaIngreso { get; set; } = DateTime.Now;
    public int NivelTriage { get; set; } = 3;
    public string Sintomas { get; set; } = "";
}
public static class Validacion
{
    public static readonly string[] TiposSangre = { "No conocido", "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };
    public static void Carnet(CarneCURP p)
    {
        ArgumentNullException.ThrowIfNull(p);
        p.Curp = (p.Curp ?? "").Trim().ToUpperInvariant();
        p.NombreCompleto = (p.NombreCompleto ?? "").Trim();
        if (!Regex.IsMatch(p.Curp, @"^[A-Z][AEIOUX][A-Z]{2}\d{6}[HM][A-Z]{5}[A-Z0-9]\d$"))
            throw new ArgumentException("Escribe una CURP de 18 caracteres con formato válido. Esta revisión no verifica su autenticidad.");
        if (p.NombreCompleto.Length < 3 || p.NombreCompleto.Length > 150) throw new ArgumentException("Escribe el nombre completo (3 a 150 caracteres).");
        if (p.FechaNacimiento < new DateOnly(1900, 1, 1)) throw new ArgumentException("Revisa la fecha de nacimiento (desde 1900).");
        _ = p.CalcularEdad();
        if (!TiposSangre.Contains(p.TipoSangre)) throw new ArgumentException("Selecciona un tipo de sangre de la lista.");
        if (p.Residencia is null || p.HistorialConsultas is null || p.HistorialUrgencias is null) throw new ArgumentException("El carnet contiene datos incompletos.");
        if (string.IsNullOrWhiteSpace(p.Folio) || p.Folio.Length > 80) throw new ArgumentException("El folio es obligatorio y admite hasta 80 caracteres.");
        if (p.Numtel is null || (p.Numtel.Length > 0 && !Regex.IsMatch(p.Numtel, @"^\+?[0-9 ()-]{7,25}$"))) throw new ArgumentException("Revisa el teléfono; puedes usar +, dígitos, espacios y guiones.");
        if (p.Residencia.codpost is null || (p.Residencia.codpost.Length > 0 && !Regex.IsMatch(p.Residencia.codpost, @"^\d{5}$"))) throw new ArgumentException("El código postal debe tener 5 dígitos o quedar vacío.");
        if (new[] { p.Residencia.callenumero, p.Residencia.colonia, p.Residencia.alcaldiamunicipio }.Any(x => x is null || x.Length > 200)) throw new ArgumentException("Revisa la dirección (hasta 200 caracteres por campo).");
        foreach (var c in p.HistorialConsultas) { if (c is null) throw new ArgumentException("Consulta vacía."); Consulta(c); }
        foreach (var u in p.HistorialUrgencias) { if (u is null) throw new ArgumentException("Urgencia vacía."); Urgencia(u); }
        if (p.HistorialConsultas.Select(c => c.Id).Distinct().Count() != p.HistorialConsultas.Count || p.HistorialUrgencias.Select(c => c.Id).Distinct().Count() != p.HistorialUrgencias.Count) throw new ArgumentException("Hay registros repetidos en el historial.");
    }
    public static void Consulta(CitaConsulta c)
    {
        if (c.Id == Guid.Empty || c.Fecha.Year < 1900 || string.IsNullOrWhiteSpace(c.MedicoAsignado) || string.IsNullOrWhiteSpace(c.Consultorio) || c.MedicoAsignado.Length > 150 || c.Consultorio.Length > 80)
            throw new ArgumentException("Indica fecha, médico y consultorio válidos.");
    }
    public static void Urgencia(RegistroUrgencia u)
    {
        if (u.Id == Guid.Empty || u.FechaIngreso.Year < 1900 || u.FechaIngreso > DateTime.Now.AddMinutes(1) || u.NivelTriage < 1 || u.NivelTriage > 5 || string.IsNullOrWhiteSpace(u.Sintomas) || u.Sintomas.Length > 2000)
            throw new ArgumentException("Indica una fecha de ingreso no futura, nivel del 1 al 5 y síntomas (hasta 2000 caracteres).");
    }
}
