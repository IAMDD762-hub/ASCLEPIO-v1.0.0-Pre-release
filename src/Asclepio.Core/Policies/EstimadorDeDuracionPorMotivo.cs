using Asclepio.Core.Abstractions;
using Asclepio.Core.Domain;

namespace Asclepio.Core.Policies;

/// <summary>
/// Arranque en frío con una duración base; conforme se cierran consultas aprende por motivo
/// (últimas 50 muestras). Mediana para estimar esperas, percentil 80 para asignar (nota *7).
/// </summary>
public sealed class EstimadorDeDuracionPorMotivo : IEstimadorDeDuracion, IRegistradorDeDuraciones
{
    private const int MaxMuestras = 50;
    private const int MinMuestras = 5;

    private readonly TimeSpan _medianaBase;
    private readonly TimeSpan _p80Base;
    private readonly Dictionary<string, Queue<double>> _muestras = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _candado = new();

    public EstimadorDeDuracionPorMotivo(TimeSpan? medianaBase = null, TimeSpan? p80Base = null)
    {
        _medianaBase = medianaBase ?? TimeSpan.FromMinutes(15);
        _p80Base = p80Base ?? TimeSpan.FromMinutes(22);
    }

    public TimeSpan Mediana(Paciente paciente) => Percentil(paciente.Motivo, 0.5, _medianaBase);

    public TimeSpan P80(Paciente paciente) => Percentil(paciente.Motivo, 0.8, _p80Base);

    public void RegistrarReal(string motivo, TimeSpan duracion)
    {
        if (duracion <= TimeSpan.Zero) return;

        lock (_candado)
        {
            if (!_muestras.TryGetValue(motivo, out var cola))
                _muestras[motivo] = cola = new Queue<double>();

            cola.Enqueue(duracion.TotalMinutes);
            while (cola.Count > MaxMuestras) cola.Dequeue();
        }
    }

    private TimeSpan Percentil(string motivo, double p, TimeSpan valorBase)
    {
        double[] ordenadas;
        lock (_candado)
        {
            if (!_muestras.TryGetValue(motivo, out var cola) || cola.Count < MinMuestras) return valorBase;
            ordenadas = cola.OrderBy(x => x).ToArray();
        }

        var indice = Math.Clamp((int)Math.Ceiling(p * ordenadas.Length) - 1, 0, ordenadas.Length - 1);
        return TimeSpan.FromMinutes(ordenadas[indice]);
    }
}
