using Asclepio.Core.Abstractions;
using Asclepio.Core.Domain;
using Asclepio.Core.Engine;

namespace Asclepio.Core.Policies;

/// <summary>
/// Cierre de la Figura 2: ¿la espera supera el objetivo del nivel, o hay cola sin médicos?
/// El segundo caso es el invariante de cobertura del Mandamiento 4 (nota *8).
/// </summary>
public sealed class MonitorDeAlertasPorDefecto : IMonitorDeAlertas
{
    private readonly IObjetivosDeEspera _objetivos;

    public MonitorDeAlertasPorDefecto(IObjetivosDeEspera objetivos) => _objetivos = objetivos;

    public IReadOnlyList<Alerta> Evaluar(ContextoDeAlertas contexto)
    {
        var alertas = new List<Alerta>();
        if (contexto.ColaOrdenada.Count == 0) return alertas;

        var hayMedicos = contexto.Medicos.Any(m =>
            (m.Estado is EstadoMedico.Disponible or EstadoMedico.Ocupado) &&
            (m.Roles.HasFlag(RolMedico.General) || m.Roles.HasFlag(RolMedico.Especialidad)));

        if (!hayMedicos)
        {
            alertas.Add(new Alerta(
                TipoDeAlerta.ColaSinMedicos,
                $"Hay {contexto.ColaOrdenada.Count} paciente(s) esperando y ningún médico de consulta en servicio."));
            return alertas;
        }

        foreach (var p in contexto.ColaOrdenada)
        {
            if (!contexto.Esperas.TryGetValue(p.Curp, out var rango)) continue;

            var esperado = (contexto.Ahora - p.HoraLlegada) + rango.P50;
            var objetivo = _objetivos.Objetivo(p.Gravedad);
            if (esperado > objetivo)
            {
                alertas.Add(new Alerta(
                    TipoDeAlerta.EsperaSobreObjetivo,
                    $"{p.Curp} ({p.Gravedad}): espera proyectada {esperado.TotalMinutes:0} min, objetivo {objetivo.TotalMinutes:0} min.",
                    p.Curp));
            }
        }
        return alertas;
    }
}
