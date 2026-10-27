using Asclepio.Core.Abstractions;
using Asclepio.Core.Domain;

namespace Asclepio.Core.Engine;

/// <summary>
/// Orquestador de la Figura 2. No contiene reglas: solo llama, en orden, a las piezas que sí las tienen
/// (Dependency Inversion). Urgencias y el diagrama maestro podrán reutilizar estas mismas piezas.
/// Un ciclo = un evento: estado → modo → asignar (con relleno) → espera estimada → alertas.
/// </summary>
public sealed class MotorDeTiempo : IMotorDeTiempo
{
    private readonly IRepositorioDeMedicos _medicos;
    private readonly IColaDeEspera _cola;
    private readonly IActualizadorDeEstado _actualizador;
    private readonly IProveedorDeMedidas _medidas;
    private readonly IEvaluadorDeModo _evaluadorDeModo;
    private readonly ICalculadorDePrioridad _prioridad;
    private readonly IAsignadorDePaciente _asignador;
    private readonly IEstrategiaDeRelleno _relleno;
    private readonly IEstimadorDeEspera _estimadorDeEspera;
    private readonly IMonitorDeAlertas _monitor;
    private readonly INotificador _notificador;

    private readonly object _candado = new();
    private ModoOperacion _modo = ModoOperacion.M2Base;
    private bool _criticoPendiente;
    private readonly bool _requiereConfirmacionCritica;

    public MotorDeTiempo(
        IRepositorioDeMedicos medicos,
        IColaDeEspera cola,
        IActualizadorDeEstado actualizador,
        IProveedorDeMedidas medidas,
        IEvaluadorDeModo evaluadorDeModo,
        ICalculadorDePrioridad prioridad,
        IAsignadorDePaciente asignador,
        IEstrategiaDeRelleno relleno,
        IEstimadorDeEspera estimadorDeEspera,
        IMonitorDeAlertas monitor,
        INotificador notificador,
        bool requiereConfirmacionCritica = true)
    {
        _medicos = medicos;
        _cola = cola;
        _actualizador = actualizador;
        _medidas = medidas;
        _evaluadorDeModo = evaluadorDeModo;
        _prioridad = prioridad;
        _asignador = asignador;
        _relleno = relleno;
        _estimadorDeEspera = estimadorDeEspera;
        _monitor = monitor;
        _notificador = notificador;
        _requiereConfirmacionCritica = requiereConfirmacionCritica;
    }

    public ModoOperacion ModoActivo
    {
        get { lock (_candado) return _modo; }
    }

    public void ConfirmarModoCritico()
    {
        lock (_candado)
        {
            if (!_criticoPendiente) return;
            _modo = ModoOperacion.M6Critico;
            _criticoPendiente = false;
        }
    }

    public ResultadoDelCiclo Procesar(EventoDelSistema evento)
    {
        lock (_candado)
        {
            var ahora = evento.Momento;
            var traza = new List<string> { $"Evento {evento.Tipo} a las {ahora:HH:mm:ss}" };
            var asignaciones = new List<Asignacion>();
            var alertas = new List<Alerta>();

            // 1. Actualizar el estado de cada médico.
            var medicos = _medicos.ObtenerTodos();
            _actualizador.Actualizar(medicos, ahora);

            // 2. Recalcular el modo (con histéresis).
            ActualizarModo(ahora, traza, alertas);

            // 3-7. Asignar en orden de prioridad; si el primero no cabe, relleno con el resto.
            var pendientes = Ordenar(_cola.Pacientes, ahora);
            traza.Add($"Cola: {pendientes.Count} paciente(s); modo {_modo}");

            while (pendientes.Count > 0)
            {
                var p = pendientes[0];
                var asignacion = _asignador.IntentarAsignar(p, _modo, ahora);

                if (asignacion is not null)
                {
                    Registrar(asignacion, asignaciones, traza);
                    pendientes.RemoveAt(0);
                    continue;
                }

                traza.Add($"{p.Curp} ({p.Gravedad}) no cabe en ninguna ventana.");

                // Si ningún médico está disponible ahora no hay nada que rellenar.
                if (medicos.Any(m => m.Estado == EstadoMedico.Disponible))
                {
                    var restantes = pendientes.Skip(1).ToList();
                    foreach (var r in _relleno.Rellenar(restantes, q => _asignador.IntentarAsignar(q, _modo, ahora)))
                    {
                        traza.Add("Relleno: ");
                        Registrar(r, asignaciones, traza);
                    }
                }
                break;
            }

            // 8. Espera estimada de quienes siguen en cola, y alertas.
            var ordenada = Ordenar(_cola.Pacientes, ahora);
            var esperas = _estimadorDeEspera.Estimar(ordenada, medicos, ahora);
            _notificador.EsperasActualizadas(esperas);

            var contexto = new ContextoDeAlertas(ordenada, medicos, esperas, ahora, _modo);
            foreach (var a in _monitor.Evaluar(contexto))
            {
                alertas.Add(a);
                _notificador.AlertaEmitida(a);
            }

            return new ResultadoDelCiclo(_modo, asignaciones, esperas, alertas, traza);
        }
    }

    private void ActualizarModo(DateTime ahora, List<string> traza, List<Alerta> alertas)
    {
        var propuesto = _evaluadorDeModo.Evaluar(_medidas.Obtener(ahora), _modo, ahora);

        if (propuesto == _modo)
        {
            if (_modo != ModoOperacion.M6Critico) _criticoPendiente = false;
            return;
        }

        if (propuesto == ModoOperacion.M6Critico && _requiereConfirmacionCritica)
        {
            // Nota *15: el sistema propone, el responsable de la unidad confirma.
            _criticoPendiente = true;
            var alerta = new Alerta(TipoDeAlerta.ModoCriticoPorConfirmar,
                "Se propone el Modo Crítico (M6). Requiere confirmación del responsable de la unidad.");
            alertas.Add(alerta);
            _notificador.AlertaEmitida(alerta);
            traza.Add("Modo M6 propuesto; pendiente de confirmación.");
            return;
        }

        traza.Add($"Cambio de modo: {_modo} → {propuesto}");
        _modo = propuesto;
        _criticoPendiente = false;
    }

    private void Registrar(Asignacion asignacion, List<Asignacion> asignaciones, List<string> traza)
    {
        _cola.Quitar(asignacion.Paciente.Curp);
        asignaciones.Add(asignacion);
        _notificador.AsignacionRealizada(asignacion);
        traza.Add($"Asignado {asignacion.Paciente.Curp} → {asignacion.Medico.Nombre} " +
                  $"({asignacion.Recurso}), ~{asignacion.DuracionEstimada.TotalMinutes:0} min");
    }

    private List<Paciente> Ordenar(IReadOnlyList<Paciente> cola, DateTime ahora)
        => cola.OrderByDescending(p => _prioridad.Puntaje(p, ahora))
               .ThenBy(p => p.HoraLlegada)
               .ToList();
}
