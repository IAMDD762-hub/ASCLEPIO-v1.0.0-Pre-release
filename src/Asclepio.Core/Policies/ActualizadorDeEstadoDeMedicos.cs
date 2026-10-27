using Asclepio.Core.Abstractions;
using Asclepio.Core.Domain;

namespace Asclepio.Core.Policies;

/// <summary>
/// Paso 1. Si un médico ya rebasó su T_libre y sigue ocupado, se reestima con un tiempo restante
/// esperado (la gota tarda más de lo previsto). Valor de ejemplo: 5 minutos más.
/// </summary>
public sealed class ActualizadorDeEstadoDeMedicos : IActualizadorDeEstado
{
    private readonly TimeSpan _restanteTrasRebasar;

    public ActualizadorDeEstadoDeMedicos(TimeSpan? restanteTrasRebasar = null)
        => _restanteTrasRebasar = restanteTrasRebasar ?? TimeSpan.FromMinutes(5);

    public void Actualizar(IReadOnlyList<Medico> medicos, DateTime ahora)
    {
        foreach (var m in medicos)
        {
            if (m.Estado == EstadoMedico.Ocupado && m.LibreEstimadoA is { } t && t < ahora)
                m.ActualizarLibreEstimado(ahora + _restanteTrasRebasar);
        }
    }
}
