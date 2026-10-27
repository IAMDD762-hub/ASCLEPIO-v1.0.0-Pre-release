using Asclepio.Core.Abstractions;
using Asclepio.Core.Domain;

namespace Asclepio.Core.Engine;

/// <summary>
/// Pasos 3 a 6 de la Figura 2 para UN paciente:
///  3. estimar duración (P80);  4. filtrar médicos elegibles;  5. elegir el mejor ajuste;
///  6. asignar, reservar recurso y marcar al médico como ocupado.
/// Si no hay elegibles, prueba los escalones de flexibilidad del modo uno por uno ("¿el modo permite flexibilizar?").
/// No decide el orden de la cola ni avisa a nadie: eso es del motor.
/// </summary>
public sealed class AsignadorDePaciente : IAsignadorDePaciente
{
    private readonly IRepositorioDeMedicos _medicos;
    private readonly IEstimadorDeDuracion _duracion;
    private readonly IServicioDeVentanas _ventanas;
    private readonly IReglaDeCompatibilidad _compatibilidad;
    private readonly ISelectorDeMedico _selector;
    private readonly IPoliticaDeModo _politica;
    private readonly IGestorDeRecursos _recursos;

    public AsignadorDePaciente(
        IRepositorioDeMedicos medicos,
        IEstimadorDeDuracion duracion,
        IServicioDeVentanas ventanas,
        IReglaDeCompatibilidad compatibilidad,
        ISelectorDeMedico selector,
        IPoliticaDeModo politica,
        IGestorDeRecursos recursos)
    {
        _medicos = medicos;
        _duracion = duracion;
        _ventanas = ventanas;
        _compatibilidad = compatibilidad;
        _selector = selector;
        _politica = politica;
        _recursos = recursos;
    }

    public Asignacion? IntentarAsignar(Paciente paciente, ModoOperacion modo, DateTime ahora)
    {
        if (!_recursos.HayDisponible()) return null;

        var todos = _medicos.ObtenerTodos();

        foreach (var parametros in _politica.Escalones(modo))
        {
            var s80 = Escalar(_duracion.P80(paciente), parametros.FactorDuracion);
            var margen = TimeSpan.FromMinutes(parametros.MargenMin);

            // Paso 4: disponible ahora, rol compatible y ventana W(m) >= S(p) + margen.
            var elegibles = todos
                .Where(m => m.Estado == EstadoMedico.Disponible
                         && _compatibilidad.Compatible(m, paciente, parametros)
                         && _ventanas.Ventana(m, ahora) >= s80 + margen)
                .ToList();

            if (elegibles.Count == 0) continue; // probar el siguiente escalón de flexibilidad

            // Paso 5: mejor ajuste.
            var elegido = _selector.Elegir(elegibles, paciente, s80, ahora);
            if (elegido is null) continue;

            // Paso 6: reservar y asignar. La "gota" se estima con la mediana, no con el P80.
            var recurso = _recursos.Reservar(elegido.Id);
            if (recurso is null) return null;

            var estimada = Escalar(_duracion.Mediana(paciente), parametros.FactorDuracion);
            elegido.Ocupar(paciente.Curp, paciente.Motivo, ahora, estimada);
            return new Asignacion(paciente, elegido, recurso, ahora, estimada);
        }

        return null;
    }

    private static TimeSpan Escalar(TimeSpan t, double factor) => TimeSpan.FromTicks((long)(t.Ticks * factor));
}
