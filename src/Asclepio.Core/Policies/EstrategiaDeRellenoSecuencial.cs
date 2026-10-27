using Asclepio.Core.Abstractions;
using Asclepio.Core.Domain;
using Asclepio.Core.Engine;

namespace Asclepio.Core.Policies;

/// <summary>
/// Paso 7 (backfilling). Cuando el paciente de mayor prioridad no cabe en ninguna ventana, se recorre al resto
/// de la cola en orden e intenta colocar a quien sí quepa. No retrasa al primero: los médicos que están
/// disponibles ahora ya se descartaron para él por falta de ventana.
/// </summary>
public sealed class EstrategiaDeRellenoSecuencial : IEstrategiaDeRelleno
{
    public IReadOnlyList<Asignacion> Rellenar(
        IReadOnlyList<Paciente> restantes, Func<Paciente, Asignacion?> intentar)
    {
        var colocados = new List<Asignacion>();
        foreach (var paciente in restantes)
        {
            var asignacion = intentar(paciente);
            if (asignacion is not null) colocados.Add(asignacion);
        }
        return colocados;
    }
}
