using Asclepio.Core.Domain;
using Asclepio.Core.Engine;

namespace Asclepio.Core.Abstractions;

// Puertos hacia el exterior: lo que el motor NECESITA del mundo (reloj, datos, avisos).
// La interfaz de escritorio, Firebase, el módulo de carnet-CURP, etc. implementan estos contratos.

public interface IReloj
{
    DateTime Ahora { get; }
}

public interface IRepositorioDeMedicos
{
    IReadOnlyList<Medico> ObtenerTodos();
    Medico? Obtener(string id);
}

public interface IColaDeEspera
{
    IReadOnlyList<Paciente> Pacientes { get; }
    void Agregar(Paciente paciente);
    bool Quitar(string curp);
    bool Contiene(string curp);
}

/// <summary>El módulo del carnet-CURP lo implementa otra persona. El motor solo pide los datos por CURP.</summary>
public interface IServicioDeCarnet
{
    DatosDeCarnet? Obtener(string curp);
}

/// <summary>Recursos no humanos (consultorios, camas, equipos).</summary>
public interface IGestorDeRecursos
{
    bool HayDisponible();
    string? Reservar(string medicoId);
    void Liberar(string medicoId);
}

public interface INotificador
{
    void AsignacionRealizada(Asignacion asignacion);
    void EsperasActualizadas(IReadOnlyDictionary<string, RangoDeEspera> esperas);
    void AlertaEmitida(Alerta alerta);
}
