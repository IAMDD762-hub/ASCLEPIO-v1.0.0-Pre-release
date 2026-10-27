using Asclepio.Core.Abstractions;
using Asclepio.Core.Domain;
using Asclepio.Core.Engine;

namespace Asclepio.Core.Policies;

/// <summary>
/// Paso 8. Simula la cola en orden de prioridad sobre las líneas de tiempo de los médicos, dos veces:
/// con duraciones medianas (P50) y con duraciones P80. Respeta el próximo bloque fijo de cada médico.
/// Simplificación de la v0.1: solo cuentan médicos con rol General (no aplica la palanca de especialistas).
/// Si ningún médico cabe para un paciente, ese paciente queda sin estimación en lugar de inventar una.
/// </summary>
public sealed class EstimadorDeEsperaPorSimulacion : IEstimadorDeEspera
{
    private readonly IEstimadorDeDuracion _duracion;

    public EstimadorDeEsperaPorSimulacion(IEstimadorDeDuracion duracion) => _duracion = duracion;

    public IReadOnlyDictionary<string, RangoDeEspera> Estimar(
        IReadOnlyList<Paciente> colaOrdenada, IReadOnlyList<Medico> medicos, DateTime ahora)
    {
        var p50 = Simular(colaOrdenada, medicos, ahora, usarP80: false);
        var p80 = Simular(colaOrdenada, medicos, ahora, usarP80: true);

        var resultado = new Dictionary<string, RangoDeEspera>();
        foreach (var paciente in colaOrdenada)
        {
            if (p50.TryGetValue(paciente.Curp, out var a) && p80.TryGetValue(paciente.Curp, out var b))
                resultado[paciente.Curp] = new RangoDeEspera(a, b < a ? a : b);
        }
        return resultado;
    }

    private Dictionary<string, TimeSpan> Simular(
        IReadOnlyList<Paciente> cola, IReadOnlyList<Medico> medicos, DateTime ahora, bool usarP80)
    {
        // Línea de tiempo simulada: cuándo queda libre cada médico atendible.
        var libreEn = new Dictionary<string, DateTime>();
        var bloque = new Dictionary<string, DateTime?>();

        foreach (var m in medicos)
        {
            var atendible = m.Roles.HasFlag(RolMedico.General) &&
                            (m.Estado is EstadoMedico.Disponible or EstadoMedico.Ocupado);
            if (!atendible) continue;

            libreEn[m.Id] = m.TLibre(ahora);
            bloque[m.Id] = m.ProximoBloqueFijo;
        }

        var esperas = new Dictionary<string, TimeSpan>();
        foreach (var p in cola)
        {
            var duracion = usarP80 ? _duracion.P80(p) : _duracion.Mediana(p);

            string? mejor = null;
            var mejorInicio = DateTime.MaxValue;
            foreach (var (id, libre) in libreEn)
            {
                if (libre == DateTime.MaxValue) continue;                   // sin T_libre conocido
                if (bloque[id] is { } b && libre + duracion > b) continue; // no cabe antes de su bloque fijo
                if (libre < mejorInicio)
                {
                    mejorInicio = libre;
                    mejor = id;
                }
            }

            if (mejor is null) continue;

            esperas[p.Curp] = mejorInicio - ahora;
            libreEn[mejor] = mejorInicio + duracion;
        }
        return esperas;
    }
}
