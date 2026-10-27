using Asclepio.Core.Abstractions;
using Asclepio.Core.Domain;
using Asclepio.Core.Engine;

namespace Asclepio.Core.Infrastructure;

// Implementaciones en memoria para la versión inicial, las pruebas y la demo.
// Cuando exista base de datos (p. ej. Firebase) se escriben otras implementaciones de los mismos
// contratos y el motor no se entera.

public sealed class SistemaReloj : IReloj
{
    public DateTime Ahora => DateTime.Now;
}

/// <summary>Reloj controlable a mano: permite simular horas de consulta en segundos.</summary>
public sealed class RelojManual : IReloj
{
    public RelojManual(DateTime inicio) => Ahora = inicio;

    public DateTime Ahora { get; private set; }

    public void Avanzar(TimeSpan tiempo) => Ahora += tiempo;
}

public sealed class RepositorioDeMedicosEnMemoria : IRepositorioDeMedicos
{
    private readonly List<Medico> _medicos = new();

    public RepositorioDeMedicosEnMemoria(IEnumerable<Medico>? medicos = null)
    {
        if (medicos is not null) _medicos.AddRange(medicos);
    }

    public void Agregar(Medico medico) => _medicos.Add(medico);

    public IReadOnlyList<Medico> ObtenerTodos() => _medicos;

    public Medico? Obtener(string id) => _medicos.FirstOrDefault(m => m.Id == id);
}

public sealed class ColaDeEsperaEnMemoria : IColaDeEspera
{
    private readonly List<Paciente> _pacientes = new();

    public IReadOnlyList<Paciente> Pacientes => _pacientes;

    public void Agregar(Paciente paciente)
    {
        if (!Contiene(paciente.Curp)) _pacientes.Add(paciente);
    }

    public bool Quitar(string curp) => _pacientes.RemoveAll(p => p.Curp == curp) > 0;

    public bool Contiene(string curp) => _pacientes.Any(p => p.Curp == curp);
}

/// <summary>Un conjunto fijo de consultorios; cada médico ocupa uno mientras atiende.</summary>
public sealed class GestorDeRecursosEnMemoria : IGestorDeRecursos
{
    private readonly Queue<string> _libres = new();
    private readonly Dictionary<string, string> _porMedico = new();

    public GestorDeRecursosEnMemoria(int consultorios)
    {
        for (var i = 1; i <= consultorios; i++) _libres.Enqueue($"Consultorio {i}");
    }

    public bool HayDisponible() => _libres.Count > 0;

    public string? Reservar(string medicoId)
    {
        if (_porMedico.TryGetValue(medicoId, out var actual)) return actual;
        if (_libres.Count == 0) return null;
        var recurso = _libres.Dequeue();
        _porMedico[medicoId] = recurso;
        return recurso;
    }

    public void Liberar(string medicoId)
    {
        if (_porMedico.Remove(medicoId, out var recurso)) _libres.Enqueue(recurso);
    }
}

public sealed class NotificadorNulo : INotificador
{
    public void AsignacionRealizada(Asignacion asignacion) { }
    public void EsperasActualizadas(IReadOnlyDictionary<string, RangoDeEspera> esperas) { }
    public void AlertaEmitida(Alerta alerta) { }
}

/// <summary>Medidas controlables a mano (interfaz, pruebas). Sustituible por un proveedor con histórico real.</summary>
public sealed class ProveedorDeMedidasFijo : IProveedorDeMedidas
{
    public double Afluencia { get; set; } = 1.0;
    public double Personal { get; set; } = 1.0;

    public MedidasDeCarga Obtener(DateTime ahora) => new(Afluencia, Personal);
}

/// <summary>
/// Heurística provisional hasta contar con histórico por franja horaria (nota *13):
/// afluencia = cola actual ÷ cola típica; personal = médicos atendibles activos ÷ médicos esperados.
/// </summary>
public sealed class ProveedorDeMedidasPorEstado : IProveedorDeMedidas
{
    private readonly IColaDeEspera _cola;
    private readonly IRepositorioDeMedicos _medicos;
    private readonly int _medicosEsperados;
    private readonly int _colaTipica;

    public ProveedorDeMedidasPorEstado(
        IColaDeEspera cola, IRepositorioDeMedicos medicos, int medicosEsperados, int colaTipica)
    {
        _cola = cola;
        _medicos = medicos;
        _medicosEsperados = Math.Max(1, medicosEsperados);
        _colaTipica = Math.Max(1, colaTipica);
    }

    public MedidasDeCarga Obtener(DateTime ahora)
    {
        var activos = _medicos.ObtenerTodos().Count(m =>
            (m.Estado is EstadoMedico.Disponible or EstadoMedico.Ocupado) &&
            (m.Roles.HasFlag(RolMedico.General) || m.Roles.HasFlag(RolMedico.Especialidad)));

        return new MedidasDeCarga(
            Afluencia: _cola.Pacientes.Count / (double)_colaTipica,
            Personal: activos / (double)_medicosEsperados);
    }
}
