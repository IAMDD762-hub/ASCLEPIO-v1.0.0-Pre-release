using Asclepio.Core.Abstractions;
using Asclepio.Core.Domain;

namespace Asclepio.Core.Infrastructure;

/// <summary>
/// SUSTITUTO TEMPORAL del módulo carnet-CURP que construye otra persona del equipo.
/// Cuando ese módulo exista, basta con que implemente <see cref="IServicioDeCarnet"/> y se pase
/// al constructor del núcleo; este archivo se puede borrar.
/// </summary>
public sealed class ServicioDeCarnetSimulado : IServicioDeCarnet
{
    private readonly Dictionary<string, DatosDeCarnet> _carnets = new();

    /// <summary>Si es true, cualquier CURP "existe" con una clase por defecto (cómodo para demos).</summary>
    public bool CrearAutomaticamente { get; set; } = true;

    public void Registrar(DatosDeCarnet datos) => _carnets[datos.Curp.ToUpperInvariant()] = datos;

    public DatosDeCarnet? Obtener(string curp)
    {
        if (_carnets.TryGetValue(curp, out var datos)) return datos;
        return CrearAutomaticamente ? new DatosDeCarnet(curp, ClasePaciente.AgudoMayor, null) : null;
    }
}
