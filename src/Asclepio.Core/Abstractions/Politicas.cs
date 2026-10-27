using Asclepio.Core.Domain;
using Asclepio.Core.Engine;

namespace Asclepio.Core.Abstractions;

// Políticas reemplazables (Open/Closed): cada una es una pieza del diagrama de la Figura 2.
// Cambiar el score, el estimador o la selección es sustituir una clase, no tocar el motor.

/// <summary>Paso 3: duración estimada de consulta. P80 para asignar (no desbordar la próxima cita), mediana para estimar esperas.</summary>
public interface IEstimadorDeDuracion
{
    TimeSpan Mediana(Paciente paciente);
    TimeSpan P80(Paciente paciente);
}

/// <summary>Cierre de consulta: alimenta las estimaciones con tiempos reales. Separada de la lectura (ISP).</summary>
public interface IRegistradorDeDuraciones
{
    void RegistrarReal(string motivo, TimeSpan duracion);
}

public interface IObjetivosDeEspera
{
    TimeSpan Objetivo(NivelGravedad gravedad);
}

/// <summary>Score de prioridad (documento, sección 12). Mayor puntaje = se atiende antes.</summary>
public interface ICalculadorDePrioridad
{
    double Puntaje(Paciente paciente, DateTime ahora);
}

/// <summary>W(m): tiempo libre del médico antes de su próximo bloque fijo. TimeSpan.MaxValue si no tiene bloque conocido.</summary>
public interface IServicioDeVentanas
{
    TimeSpan Ventana(Medico medico, DateTime ahora);
}

/// <summary>Paso 4 (parte "rol compatible").</summary>
public interface IReglaDeCompatibilidad
{
    bool Compatible(Medico medico, Paciente paciente, ParametrosDeModo parametros);
}

/// <summary>Paso 5: de los elegibles, cuál se lleva al paciente.</summary>
public interface ISelectorDeMedico
{
    Medico? Elegir(IReadOnlyList<Medico> elegibles, Paciente paciente, TimeSpan duracion, DateTime ahora);
}

/// <summary>
/// Qué palancas permite cada modo. El primer elemento es la configuración base del modo; los siguientes
/// son escalones de flexibilidad más agresivos que el motor prueba UNO POR UNO cuando no hay elegibles.
/// </summary>
public interface IPoliticaDeModo
{
    IReadOnlyList<ParametrosDeModo> Escalones(ModoOperacion modo);
}

public interface IProveedorDeMedidas
{
    MedidasDeCarga Obtener(DateTime ahora);
}

/// <summary>Paso 1: poner al día el estado de cada médico (p. ej. reestimar a quien ya rebasó su T_libre).</summary>
public interface IActualizadorDeEstado
{
    void Actualizar(IReadOnlyList<Medico> medicos, DateTime ahora);
}

/// <summary>Paso 2: recalcular el modo, con histéresis para que no oscile.</summary>
public interface IEvaluadorDeModo
{
    ModoOperacion Evaluar(MedidasDeCarga medidas, ModoOperacion actual, DateTime ahora);
}

/// <summary>Paso 8: espera estimada (P50-P80) de todos los que siguen en cola.</summary>
public interface IEstimadorDeEspera
{
    IReadOnlyDictionary<string, RangoDeEspera> Estimar(
        IReadOnlyList<Paciente> colaOrdenada, IReadOnlyList<Medico> medicos, DateTime ahora);
}

public interface IMonitorDeAlertas
{
    IReadOnlyList<Alerta> Evaluar(ContextoDeAlertas contexto);
}

/// <summary>Pasos 3-6 para UN paciente: estimar, filtrar elegibles, elegir mejor ajuste, asignar y reservar.</summary>
public interface IAsignadorDePaciente
{
    Asignacion? IntentarAsignar(Paciente paciente, ModoOperacion modo, DateTime ahora);
}

/// <summary>Paso 7: relleno (backfilling) con los pacientes que siguen en la cola.</summary>
public interface IEstrategiaDeRelleno
{
    IReadOnlyList<Asignacion> Rellenar(IReadOnlyList<Paciente> restantes, Func<Paciente, Asignacion?> intentar);
}

/// <summary>Contrato público del motor. Urgencias y el diagrama maestro lo usarán o lo envolverán.</summary>
public interface IMotorDeTiempo
{
    ModoOperacion ModoActivo { get; }
    ResultadoDelCiclo Procesar(EventoDelSistema evento);

    /// <summary>El responsable de la unidad confirma el Modo Crítico (nota *15).</summary>
    void ConfirmarModoCritico();
}
