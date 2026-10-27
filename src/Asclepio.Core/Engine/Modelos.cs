using Asclepio.Core.Domain;

namespace Asclepio.Core.Engine;

public enum TipoDeEvento
{
    LlegaPaciente,
    SeLiberaMedico,
    CancelacionONoShow,
    FinDeConsulta,
    CambiaModo,
    CambiaTurno,
    Reloj
}

/// <summary>Lo que dispara un ciclo del motor (el recuadro "EVENTO" de la Figura 2).</summary>
public sealed record EventoDelSistema(TipoDeEvento Tipo, DateTime Momento, string? Detalle = null);

/// <summary>Un paciente asignado a un médico (y a un recurso, p. ej. consultorio).</summary>
public sealed record Asignacion(
    Paciente Paciente,
    Medico Medico,
    string? Recurso,
    DateTime Inicio,
    TimeSpan DuracionEstimada);

/// <summary>Espera estimada que se muestra al paciente: siempre un rango, nunca un número único.</summary>
public sealed record RangoDeEspera(TimeSpan P50, TimeSpan P80);

public enum TipoDeAlerta
{
    EsperaSobreObjetivo,
    ColaSinMedicos,
    ModoCriticoPorConfirmar
}

public sealed record Alerta(TipoDeAlerta Tipo, string Mensaje, string? Curp = null);

public sealed record ContextoDeAlertas(
    IReadOnlyList<Paciente> ColaOrdenada,
    IReadOnlyList<Medico> Medicos,
    IReadOnlyDictionary<string, RangoDeEspera> Esperas,
    DateTime Ahora,
    ModoOperacion Modo);

/// <summary>Todo lo que dejó un ciclo: sirve a la interfaz y a las pruebas. La traza explica cada decisión.</summary>
public sealed record ResultadoDelCiclo(
    ModoOperacion Modo,
    IReadOnlyList<Asignacion> Asignaciones,
    IReadOnlyDictionary<string, RangoDeEspera> Esperas,
    IReadOnlyList<Alerta> Alertas,
    IReadOnlyList<string> Traza);
