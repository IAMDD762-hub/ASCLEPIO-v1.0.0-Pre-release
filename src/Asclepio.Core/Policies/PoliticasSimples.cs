using Asclepio.Core.Abstractions;
using Asclepio.Core.Domain;

namespace Asclepio.Core.Policies;

/// <summary>Tiempos objetivo del IMSS por color (rojo inmediato, naranja 10, amarillo 30, verde 120, azul 180 min). Nota *3.</summary>
public sealed class ObjetivosDeEsperaPorDefecto : IObjetivosDeEspera
{
    public TimeSpan Objetivo(NivelGravedad gravedad) => gravedad switch
    {
        NivelGravedad.Rojo => TimeSpan.Zero,
        NivelGravedad.Naranja => TimeSpan.FromMinutes(10),
        NivelGravedad.Amarillo => TimeSpan.FromMinutes(30),
        NivelGravedad.Verde => TimeSpan.FromMinutes(120),
        NivelGravedad.Azul => TimeSpan.FromMinutes(180),
        _ => TimeSpan.FromMinutes(180)
    };
}

/// <summary>
/// score = 100 × (6 − nivel) + 10 × rango de clase + 20 × (espera ÷ objetivo del nivel).
/// Los pesos son de ejemplo (nota *4). El tercer término crece sin límite: evita la inanición.
/// </summary>
public sealed class CalculadorDePrioridadPorDefecto : ICalculadorDePrioridad
{
    private readonly IObjetivosDeEspera _objetivos;

    public CalculadorDePrioridadPorDefecto(IObjetivosDeEspera objetivos) => _objetivos = objetivos;

    public double Puntaje(Paciente paciente, DateTime ahora)
    {
        var espera = ahora - paciente.HoraLlegada;
        if (espera < TimeSpan.Zero) espera = TimeSpan.Zero;

        // Rojo tiene objetivo 0: se usa 1 minuto para no dividir entre cero.
        var objetivo = _objetivos.Objetivo(paciente.Gravedad);
        if (objetivo < TimeSpan.FromMinutes(1)) objetivo = TimeSpan.FromMinutes(1);

        return 100.0 * (6 - (int)paciente.Gravedad)
             + 10.0 * (int)paciente.Clase
             + 20.0 * (espera.TotalMinutes / objetivo.TotalMinutes);
    }
}

public sealed class ServicioDeVentanas : IServicioDeVentanas
{
    public TimeSpan Ventana(Medico medico, DateTime ahora)
    {
        if (medico.ProximoBloqueFijo is not { } bloque) return TimeSpan.MaxValue;

        var inicio = medico.TLibre(ahora);
        if (inicio == DateTime.MaxValue) return TimeSpan.Zero;

        var ventana = bloque - inicio;
        return ventana < TimeSpan.Zero ? TimeSpan.Zero : ventana;
    }
}

/// <summary>Mandamiento 2: el especialista solo atiende consulta general si el modo lo permite.</summary>
public sealed class ReglaDeCompatibilidadPorDefecto : IReglaDeCompatibilidad
{
    public bool Compatible(Medico medico, Paciente paciente, ParametrosDeModo parametros)
    {
        if (medico.Roles.HasFlag(RolMedico.General)) return true;
        return medico.Roles.HasFlag(RolMedico.Especialidad) && parametros.EspecialistaComoGeneral;
    }
}

/// <summary>Mejor ajuste: la ventana más pequeña que alcance. Desempate: su médico de cabecera, luego menor carga.</summary>
public sealed class SelectorMejorAjuste : ISelectorDeMedico
{
    private readonly IServicioDeVentanas _ventanas;

    public SelectorMejorAjuste(IServicioDeVentanas ventanas) => _ventanas = ventanas;

    public Medico? Elegir(IReadOnlyList<Medico> elegibles, Paciente paciente, TimeSpan duracion, DateTime ahora)
    {
        return elegibles
            .OrderBy(m => _ventanas.Ventana(m, ahora))
            .ThenBy(m => m.Id == paciente.MedicoCabeceraId ? 0 : 1)
            .ThenBy(m => m.CargaAcumuladaMin)
            .FirstOrDefault();
    }
}

/// <summary>
/// Escalones de flexibilidad por modo. El motor los prueba en orden cuando no hay elegibles.
/// Valores de ejemplo (nota *13); nunca tocan los pisos de seguridad.
/// </summary>
public sealed class PoliticaDeModoPorDefecto : IPoliticaDeModo
{
    private static readonly ParametrosDeModo Base = new(MargenMin: 5, EspecialistaComoGeneral: false, FactorDuracion: 1.0);
    private static readonly ParametrosDeModo ConEspecialistas = new(5, true, 1.0);
    private static readonly ParametrosDeModo Comprimido = new(3, true, 0.9);
    private static readonly ParametrosDeModo MuyComprimido = new(2, true, 0.8);

    public IReadOnlyList<ParametrosDeModo> Escalones(ModoOperacion modo) => modo switch
    {
        ModoOperacion.M1Valle => new[] { Base },
        ModoOperacion.M2Base => new[] { Base },
        ModoOperacion.M3Pico => new[] { Base, ConEspecialistas, Comprimido },
        ModoOperacion.M4Austero => new[] { Base, ConEspecialistas },
        ModoOperacion.M5Tension => new[] { Base, ConEspecialistas, Comprimido },
        ModoOperacion.M6Critico => new[] { Base, Comprimido, MuyComprimido },
        _ => new[] { Base }
    };
}
