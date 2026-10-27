using Asclepio.Core.Abstractions;
using Asclepio.Core.Domain;
using Asclepio.Core.Engine;

namespace Asclepio.Core.Application;

/// <summary>
/// Fachada que usará la interfaz de escritorio: cada método es una acción del mundo real
/// (llega alguien, termina una consulta, no se presenta un paciente) y devuelve el resultado del ciclo.
/// La interfaz nunca habla con el motor ni con las políticas directamente.
/// Solo recibe el CURP: los datos del carnet los entrega el módulo del compañero (IServicioDeCarnet).
/// </summary>
public sealed class ServicioDeConsulta
{
    private const int LongitudCurp = 18;

    private readonly IMotorDeTiempo _motor;
    private readonly IColaDeEspera _cola;
    private readonly IRepositorioDeMedicos _medicos;
    private readonly IServicioDeCarnet _carnet;
    private readonly IReloj _reloj;
    private readonly IRegistradorDeDuraciones _duraciones;
    private readonly IGestorDeRecursos _recursos;

    public ServicioDeConsulta(
        IMotorDeTiempo motor,
        IColaDeEspera cola,
        IRepositorioDeMedicos medicos,
        IServicioDeCarnet carnet,
        IReloj reloj,
        IRegistradorDeDuraciones duraciones,
        IGestorDeRecursos recursos)
    {
        _motor = motor;
        _cola = cola;
        _medicos = medicos;
        _carnet = carnet;
        _reloj = reloj;
        _duraciones = duraciones;
        _recursos = recursos;
    }

    public ModoOperacion ModoActivo => _motor.ModoActivo;

    /// <summary>
    /// Un paciente ya pasó por la recepción-triaje médico (que fija la gravedad) y entra a consulta.
    /// Falla con una excepción clara si el CURP no es válido o no tiene carnet.
    /// </summary>
    public ResultadoDelCiclo RegistrarLlegada(string curp, NivelGravedad gravedad, string motivo)
    {
        curp = NormalizarCurp(curp);
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo de consulta es obligatorio.", nameof(motivo));

        if (!_cola.Contiene(curp))
        {
            var carnet = _carnet.Obtener(curp)
                ?? throw new KeyNotFoundException($"No existe carnet para el CURP {curp}.");

            _cola.Agregar(new Paciente(curp, gravedad, carnet.Clase, motivo.Trim(), _reloj.Ahora, carnet.MedicoCabeceraId));
        }

        return _motor.Procesar(new EventoDelSistema(TipoDeEvento.LlegaPaciente, _reloj.Ahora, curp));
    }

    /// <summary>Cierra la consulta del médico: guarda la duración real (para aprender) y libera su "gota".</summary>
    public ResultadoDelCiclo FinalizarConsulta(string medicoId)
    {
        var medico = _medicos.Obtener(medicoId)
            ?? throw new KeyNotFoundException($"No existe el médico {medicoId}.");

        if (medico.Estado != EstadoMedico.Ocupado)
            throw new InvalidOperationException($"El médico {medicoId} no tiene una consulta en curso.");

        var ahora = _reloj.Ahora;
        if (medico.MotivoActual is { } motivo && medico.InicioConsulta is { } inicio)
            _duraciones.RegistrarReal(motivo, ahora - inicio);

        medico.Liberar();
        _recursos.Liberar(medico.Id);

        return _motor.Procesar(new EventoDelSistema(TipoDeEvento.FinDeConsulta, ahora, medicoId));
    }

    /// <summary>El paciente se fue o canceló: sale de la cola y su lugar lo toma el siguiente.</summary>
    public ResultadoDelCiclo CancelarPaciente(string curp)
    {
        _cola.Quitar(NormalizarCurp(curp));
        return _motor.Procesar(new EventoDelSistema(TipoDeEvento.CancelacionONoShow, _reloj.Ahora, curp));
    }

    /// <summary>Cambia el próximo bloque fijo de un médico (cita nueva, cancelada, descanso, fin de turno).</summary>
    public ResultadoDelCiclo ActualizarBloqueFijo(string medicoId, DateTime? bloque)
    {
        var medico = _medicos.Obtener(medicoId)
            ?? throw new KeyNotFoundException($"No existe el médico {medicoId}.");

        medico.ProximoBloqueFijo = bloque;
        return _motor.Procesar(new EventoDelSistema(TipoDeEvento.CambiaTurno, _reloj.Ahora, medicoId));
    }

    /// <summary>Ciclo por reloj (p. ej. cada minuto): reestima esperas aunque no haya pasado nada.</summary>
    public ResultadoDelCiclo Refrescar()
        => _motor.Procesar(new EventoDelSistema(TipoDeEvento.Reloj, _reloj.Ahora));

    public void ConfirmarModoCritico() => _motor.ConfirmarModoCritico();

    private static string NormalizarCurp(string curp)
    {
        var limpio = (curp ?? string.Empty).Trim().ToUpperInvariant();
        if (limpio.Length != LongitudCurp)
            throw new ArgumentException($"El CURP debe tener {LongitudCurp} caracteres.", nameof(curp));
        return limpio;
    }
}
