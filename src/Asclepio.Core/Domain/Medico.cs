namespace Asclepio.Core.Domain;

[Flags]
public enum RolMedico
{
    Ninguno = 0,
    Filtro = 1,
    Urgencias = 2,
    General = 4,
    Especialidad = 8
}

public enum EstadoMedico
{
    Disponible,
    Ocupado,
    Descanso,
    FueraDeTurno,
    FueraDeServicio
}

/// <summary>
/// Un médico es una línea de tiempo: estado actual, cuándo se libera (la "gota") y
/// cuándo empieza su próximo bloque fijo (cita, descanso o fin de turno).
/// </summary>
public sealed class Medico
{
    public Medico(string id, string nombre, RolMedico roles, bool esEspecialista = false)
    {
        Id = id;
        Nombre = nombre;
        Roles = roles;
        EsEspecialista = esEspecialista;
        Estado = EstadoMedico.Disponible;
    }

    public string Id { get; }
    public string Nombre { get; }
    public RolMedico Roles { get; }
    public bool EsEspecialista { get; }

    public EstadoMedico Estado { get; private set; }

    /// <summary>T_libre: momento estimado en que termina su consulta actual (solo si está ocupado).</summary>
    public DateTime? LibreEstimadoA { get; private set; }

    /// <summary>Inicio de su próximo bloque fijo (cita, descanso o fin de turno). Null = sin bloque conocido.</summary>
    public DateTime? ProximoBloqueFijo { get; set; }

    public string? PacienteActual { get; private set; }
    public string? MotivoActual { get; private set; }

    /// <summary>Cuándo empezó la consulta en curso; con esto se mide la duración real al cerrarla.</summary>
    public DateTime? InicioConsulta { get; private set; }

    /// <summary>Minutos de consulta acumulados hoy; sirve de desempate para repartir carga.</summary>
    public double CargaAcumuladaMin { get; private set; }

    /// <summary>T_libre(m): ahora si está disponible; si está ocupado, la estimación (nunca antes de ahora).</summary>
    public DateTime TLibre(DateTime ahora)
    {
        if (Estado == EstadoMedico.Disponible) return ahora;
        if (Estado == EstadoMedico.Ocupado && LibreEstimadoA is { } t) return t > ahora ? t : ahora;
        return DateTime.MaxValue;
    }

    public void Ocupar(string curp, string motivo, DateTime inicio, TimeSpan duracionEstimada)
    {
        if (Estado != EstadoMedico.Disponible)
            throw new InvalidOperationException($"El médico {Id} no está disponible (estado: {Estado}).");

        Estado = EstadoMedico.Ocupado;
        PacienteActual = curp;
        MotivoActual = motivo;
        InicioConsulta = inicio;
        LibreEstimadoA = inicio + duracionEstimada;
        CargaAcumuladaMin += duracionEstimada.TotalMinutes;
    }

    /// <summary>Reestima el T_libre cuando la consulta ya rebasó lo estimado.</summary>
    public void ActualizarLibreEstimado(DateTime nuevoTLibre)
    {
        if (Estado == EstadoMedico.Ocupado) LibreEstimadoA = nuevoTLibre;
    }

    public void Liberar()
    {
        if (Estado != EstadoMedico.Ocupado) return;
        Estado = EstadoMedico.Disponible;
        PacienteActual = null;
        MotivoActual = null;
        InicioConsulta = null;
        LibreEstimadoA = null;
    }

    public void CambiarEstado(EstadoMedico nuevo)
    {
        if (Estado == EstadoMedico.Ocupado && nuevo != EstadoMedico.Ocupado)
            throw new InvalidOperationException("Para dejar de estar ocupado se debe llamar a Liberar().");
        Estado = nuevo;
        if (nuevo != EstadoMedico.Ocupado)
        {
            PacienteActual = null;
            MotivoActual = null;
            InicioConsulta = null;
            LibreEstimadoA = null;
        }
    }
}
