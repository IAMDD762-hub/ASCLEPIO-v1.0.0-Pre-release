namespace Asclepio.Core.Domain;

/// <summary>Nivel de gravedad 1-5 (colores del IMSS). Lo asigna la recepción-triaje médico.</summary>
public enum NivelGravedad
{
    Rojo = 1,      // inmediato
    Naranja = 2,   // 10 min
    Amarillo = 3,  // 30 min
    Verde = 4,     // 120 min
    Azul = 5       // 180 min
}

/// <summary>
/// Clase de paciente según el Mandamiento 1. El valor numérico es el "rango" que usa el score
/// (mayor = más prioritario dentro del mismo nivel de gravedad).
/// </summary>
public enum ClasePaciente
{
    CronicoMenor = 1,
    CronicoMayor = 2,
    AgudoMayor = 3,
    CronicoAgudizadoMayor = 4,
    AgudoMenor = 5,
    CronicoAgudizadoMenor = 6
}

/// <summary>Datos mínimos que el motor necesita de un paciente que espera consulta.</summary>
public sealed record Paciente(
    string Curp,
    NivelGravedad Gravedad,
    ClasePaciente Clase,
    string Motivo,
    DateTime HoraLlegada,
    string? MedicoCabeceraId = null);

/// <summary>
/// Lo que devuelve el módulo del carnet-CURP (lo construye otra persona del equipo).
/// El motor solo conoce este contrato, nunca cómo se crea o se guarda el carnet.
/// </summary>
public sealed record DatosDeCarnet(
    string Curp,
    ClasePaciente Clase,
    string? MedicoCabeceraId);
