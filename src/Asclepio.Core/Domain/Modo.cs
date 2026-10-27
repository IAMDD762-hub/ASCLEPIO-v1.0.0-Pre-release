namespace Asclepio.Core.Domain;

public enum NivelAfluencia { Baja, Normal, Excesiva }

/// <summary>Los 6 modos = personal (adecuado/escaso) × afluencia (baja/normal/excesiva).</summary>
public enum ModoOperacion
{
    M1Valle = 1,    // personal adecuado, afluencia baja
    M2Base = 2,     // personal adecuado, afluencia normal
    M3Pico = 3,     // personal adecuado, afluencia excesiva
    M4Austero = 4,  // personal escaso,   afluencia baja
    M5Tension = 5,  // personal escaso,   afluencia normal
    M6Critico = 6   // personal escaso,   afluencia excesiva
}

public static class ModoOperacionExtensiones
{
    public static ModoOperacion Combinar(bool personalEscaso, NivelAfluencia afluencia)
        => (ModoOperacion)((personalEscaso ? 3 : 0) + (int)afluencia + 1);

    public static bool PersonalEscaso(this ModoOperacion modo) => (int)modo > 3;

    public static NivelAfluencia Afluencia(this ModoOperacion modo) => (NivelAfluencia)(((int)modo - 1) % 3);
}

/// <summary>Medidas que alimentan la elección de modo (umbrales y significado en el documento, nota *13).</summary>
/// <param name="Afluencia">Llegadas esperadas próxima hora ÷ llegadas típicas de esa franja.</param>
/// <param name="Personal">Minutos-médico disponibles ÷ minutos-médico requeridos.</param>
public sealed record MedidasDeCarga(double Afluencia, double Personal);

/// <summary>
/// Palancas de flexibilidad que un modo permite. Nunca incluye los pisos de seguridad
/// (rojo/naranja inmediatos, jornadas, descansos): esos no son configurables aquí.
/// </summary>
/// <param name="MargenMin">Margen entre consultas, en minutos.</param>
/// <param name="EspecialistaComoGeneral">Mandamiento 2: el especialista puede atender consulta general.</param>
/// <param name="FactorDuracion">Multiplica la duración estimada (por ejemplo 0.85 recorta consultas).</param>
public sealed record ParametrosDeModo(
    double MargenMin,
    bool EspecialistaComoGeneral,
    double FactorDuracion);
