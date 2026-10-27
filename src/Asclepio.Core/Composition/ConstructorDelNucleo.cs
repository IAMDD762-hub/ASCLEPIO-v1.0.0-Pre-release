using Asclepio.Core.Abstractions;
using Asclepio.Core.Application;
using Asclepio.Core.Engine;
using Asclepio.Core.Infrastructure;
using Asclepio.Core.Policies;

namespace Asclepio.Core.Composition;

/// <summary>Lo que la interfaz (o la consola, o las pruebas) necesita para trabajar.</summary>
public sealed record NucleoAsclepio(
    ServicioDeConsulta Consulta,
    IMotorDeTiempo Motor,
    IColaDeEspera Cola,
    IRepositorioDeMedicos Medicos,
    IReloj Reloj);

/// <summary>
/// Raíz de composición: el ÚNICO lugar que conoce las clases concretas y las conecta.
/// Se hace a mano (sin contenedor de dependencias) a propósito: es corto, claro y no agrega paquetes.
/// Para cambiar una política basta con pasar otra implementación aquí.
/// </summary>
public static class ConstructorDelNucleo
{
    public static NucleoAsclepio CrearPorDefecto(
        IReloj reloj,
        IRepositorioDeMedicos medicos,
        IServicioDeCarnet carnet,
        IProveedorDeMedidas? medidas = null,
        INotificador? notificador = null,
        IGestorDeRecursos? recursos = null,
        IColaDeEspera? cola = null,
        IEvaluadorDeModo? evaluadorDeModo = null,
        EstimadorDeDuracionPorMotivo? duraciones = null,
        bool requiereConfirmacionCritica = true)
    {
        cola ??= new ColaDeEsperaEnMemoria();
        recursos ??= new GestorDeRecursosEnMemoria(consultorios: Math.Max(1, medicos.ObtenerTodos().Count));
        notificador ??= new NotificadorNulo();
        medidas ??= new ProveedorDeMedidasFijo();
        evaluadorDeModo ??= new EvaluadorDeModoConHisteresis();

        var objetivos = new ObjetivosDeEsperaPorDefecto();
        duraciones ??= new EstimadorDeDuracionPorMotivo();
        var ventanas = new ServicioDeVentanas();

        var asignador = new AsignadorDePaciente(
            medicos, duraciones, ventanas,
            new ReglaDeCompatibilidadPorDefecto(),
            new SelectorMejorAjuste(ventanas),
            new PoliticaDeModoPorDefecto(),
            recursos);

        var motor = new MotorDeTiempo(
            medicos, cola,
            new ActualizadorDeEstadoDeMedicos(),
            medidas, evaluadorDeModo,
            new CalculadorDePrioridadPorDefecto(objetivos),
            asignador,
            new EstrategiaDeRellenoSecuencial(),
            new EstimadorDeEsperaPorSimulacion(duraciones),
            new MonitorDeAlertasPorDefecto(objetivos),
            notificador, requiereConfirmacionCritica);

        var consulta = new ServicioDeConsulta(motor, cola, medicos, carnet, reloj, duraciones, recursos);

        return new NucleoAsclepio(consulta, motor, cola, medicos, reloj);
    }
}
