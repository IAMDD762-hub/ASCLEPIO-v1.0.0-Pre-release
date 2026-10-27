using Asclepio.Core.Abstractions;
using Asclepio.Core.Composition;
using Asclepio.Core.Domain;
using Asclepio.Core.Engine;
using Asclepio.Core.Infrastructure;
using Asclepio.Core.Policies;

namespace Asclepio.Escritorio;

// Adaptador de presentación: todos los cambios de la cola y asignaciones los hace Core.
// Datos ficticios; el reloj solo avanza cuando el presentador lo indica.
public sealed class SesionDemo
{
    public static readonly DateTime Inicio = new(2026, 10, 26, 8, 0, 0);
    public RelojManual Reloj { get; } = new(Inicio);
    public RepositorioDeMedicosEnMemoria Medicos { get; }
    public ProveedorDeMedidasFijo Medidas { get; } = new()
    {
        Afluencia = 0.50,
        Personal = 1.00
    };
    public NucleoAsclepio Nucleo { get; }
    public ResultadoDelCiclo Ultimo { get; private set; }
    public Dictionary<string, string> Nombres { get; } = new();
    public List<string> Historial { get; } = new();
    public int Atendidos { get; private set; }
    public int Paso { get; private set; }
    public const int TotalPasos = 8;
    public string Explicacion { get; private set; } =
    "Inicio de jornada. Dos médicos generales disponibles y una especialista sin habilitar para consulta general.";
    public IReadOnlyList<Paciente> ColaOrdenada => Nucleo.Cola.Pacientes
        .OrderByDescending(p => _prioridad.Puntaje(p, Reloj.Ahora)).ThenBy(p => p.HoraLlegada).ToList();
    private readonly CalculadorDePrioridadPorDefecto _prioridad = new(new ObjetivosDeEsperaPorDefecto());
    private readonly CarnetDemo _carnet = new();
    private int _numero;

    public SesionDemo()
    {
        Medicos = new(new[]
        {
            new Medico("M1", "Dra. Ruiz", RolMedico.General),
            new Medico("M2", "Dr. Pérez", RolMedico.General),
            new Medico("M3", "Dra. Soto", RolMedico.Especialidad, esEspecialista: true)
        });
        Medicos.Obtener("M1")!.ProximoBloqueFijo = Inicio.AddMinutes(25);
        var duraciones = new EstimadorDeDuracionPorMotivo();
        // Muestras sintéticas iguales para que el recorrido sea fácil de reproducir.
        foreach (var (motivo, minutos) in new[] { ("Consulta larga", 40), ("Revisión general", 15), ("Receta de continuidad", 4) })
            for (int i = 0; i < 10; i++) duraciones.RegistrarReal(motivo, TimeSpan.FromMinutes(minutos));
        Nucleo = ConstructorDelNucleo.CrearPorDefecto(
            Reloj, Medicos, _carnet, Medidas,
            evaluadorDeModo: new EvaluadorModoDemo(),
            duraciones: duraciones,
            requiereConfirmacionCritica: false); Ultimo = Nucleo.Consulta.Refrescar();
        Guardar("Escenario listo: 2 generales, 1 especialista y 3 consultorios.");
    }

    public string Nombre(string? curp) => curp is null ? "—" : Nombres.GetValueOrDefault(curp, curp);
    public double Puntaje(Paciente p) => _prioridad.Puntaje(p, Reloj.Ahora);
    public string Agregar(string nombre, NivelGravedad gravedad, string motivo)
    {
        // El prototipo de consulta expone solo niveles 3–5; urgencias queda fuera de esta pantalla.
        if (gravedad is not (NivelGravedad.Amarillo or NivelGravedad.Verde or NivelGravedad.Azul))
            throw new ArgumentException("Esta demo representa consulta: usa amarillo, verde o azul.");
        if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("Escribe un nombre ficticio para el paciente.");
        var curp = $"DEMO000101HDFXX{++_numero:000}";
        Nombres[curp] = nombre.Trim();
        _carnet.Datos[curp] = new(curp, ClasePaciente.AgudoMayor, null);
        Guardar($"Llega {nombre.Trim()} · {gravedad} · {motivo}.");
        Recibir(Nucleo.Consulta.RegistrarLlegada(curp, gravedad, motivo));
        return curp;
    }
    public void Avanzar(int minutos)
    {
        if (minutos <= 0) throw new ArgumentOutOfRangeException(nameof(minutos));
        Reloj.Avanzar(TimeSpan.FromMinutes(minutos));
        Guardar($"Reloj +{minutos} min. Las consultas siguen ocupadas hasta confirmar su fin.");
        Recibir(Nucleo.Consulta.Refrescar());
    }
    public void Finalizar(string medicoId)
    {
        var medico = Medicos.Obtener(medicoId) ?? throw new ArgumentException("Médico inexistente.");
        Guardar($"Fin de consulta de {Nombre(medico.PacienteActual)} con {medico.Nombre}.");
        Recibir(Nucleo.Consulta.FinalizarConsulta(medicoId));
        Atendidos++;
    }
    public void Cancelar(string curp)
    {
        Guardar($"Sale de la cola: {Nombre(curp)}.");
        Recibir(Nucleo.Consulta.CancelarPaciente(curp));
    }
    public void CambiarCarga(double afluencia, double personal)
    {
        if (!double.IsFinite(afluencia) || !double.IsFinite(personal) ||
            afluencia < 0 || afluencia > 100 ||
            personal < 0 || personal > 100)
        {
            throw new ArgumentException(
                "Los porcentajes deben estar entre 0 y 100.");
        }

        Medidas.Afluencia = afluencia / 100.0;
        Medidas.Personal = personal / 100.0;

        Guardar(
            $"Condiciones aplicadas: afluencia {afluencia:0}% " +
            $"y personal {personal:0}%.");

        Recibir(Nucleo.Consulta.Refrescar());

        Explicacion += $" Modo activo: {Nucleo.Consulta.ModoActivo}.";
    }
    public void ConfirmarCritico()
    {
        Nucleo.Consulta.ConfirmarModoCritico();
        Guardar("Confirmación de modo crítico solicitada por el presentador.");
        Recibir(Nucleo.Consulta.Refrescar());
    }
    public void QuitarBloque(string medicoId)
    {
        Guardar($"Se cancela el próximo bloque fijo de {Medicos.Obtener(medicoId)!.Nombre}.");
        Recibir(Nucleo.Consulta.ActualizarBloqueFijo(medicoId, null));
    }
    public void Siguiente()
    {
        if (Paso >= TotalPasos) return;

        Paso++;

        Reloj.Avanzar(
            TimeSpan.FromMinutes(Random.Shared.Next(2, 7)));

        var ocupados = Medicos.ObtenerTodos()
            .Where(m => m.Estado == EstadoMedico.Ocupado)
            .ToArray();

        var disponibles = Medicos.ObtenerTodos()
            .Where(m => m.Estado == EstadoMedico.Disponible)
            .ToArray();

        var conBloque = Medicos.ObtenerTodos()
            .Where(m => m.ProximoBloqueFijo.HasValue)
            .ToArray();

        var cola = Nucleo.Cola.Pacientes.ToArray();

        var opciones = new List<int> { 0, 1, 4, 7 };

        if (ocupados.Length > 0) opciones.Add(2);
        if (cola.Length > 0) opciones.Add(3);
        if (conBloque.Length > 0) opciones.Add(5);
        if (disponibles.Length > 0) opciones.Add(6);

        // El primer paso siempre incorpora un paciente.
        int evento = Paso == 1
            ? 0
            : opciones[Random.Shared.Next(opciones.Count)];

        switch (evento)
        {
            case 0:
                LlegadaAleatoria();
                break;

            case 1:
                int cantidad = Random.Shared.Next(2, 5);

                for (int i = 0; i < cantidad; i++)
                    LlegadaAleatoria();

                Explicacion =
                    $"Llegaron {cantidad} pacientes. " +
                    $"Quedan {Nucleo.Cola.Pacientes.Count} en espera.";
                break;

            case 2:
                Finalizar(
                    ocupados[Random.Shared.Next(ocupados.Length)].Id);
                break;

            case 3:
                Cancelar(
                    cola[Random.Shared.Next(cola.Length)].Curp);
                break;

            case 4:
                CambiarCarga(
                    Random.Shared.Next(101),
                    Random.Shared.Next(101));
                break;

            case 5:
                QuitarBloque(
                    conBloque[Random.Shared.Next(conBloque.Length)].Id);
                break;

            case 6:
                var medico =
                    disponibles[Random.Shared.Next(disponibles.Length)];

                var hora = Reloj.Ahora.AddMinutes(
                    Random.Shared.Next(10, 46));

                Guardar(
                    $"Se programa un bloque fijo para " +
                    $"{medico.Nombre} a las {hora:HH:mm}.");

                Recibir(
                    Nucleo.Consulta.ActualizarBloqueFijo(
                        medico.Id, hora));
                break;

            case 7:
                LlegadaAleatoria(NivelGravedad.Amarillo);
                break;
        }
    }

    private void LlegadaAleatoria(NivelGravedad? gravedad = null)
    {
        string[] nombres =
        {
        "Ana", "Bruno", "Carla", "Diego",
        "Elena", "Luis", "Sofía", "Mateo"
    };

        string[] motivos =
        {
        "Consulta larga",
        "Revisión general",
        "Receta de continuidad"
    };

        NivelGravedad[] niveles =
        {
        NivelGravedad.Amarillo,
        NivelGravedad.Verde,
        NivelGravedad.Azul
    };

        string nombre =
            $"{nombres[Random.Shared.Next(nombres.Length)]} {_numero + 1}";

        Agregar(
            nombre,
            gravedad ?? niveles[Random.Shared.Next(niveles.Length)],
            motivos[Random.Shared.Next(motivos.Length)]);
    }
    private void Recibir(ResultadoDelCiclo resultado)
    {
        string suceso =
            Historial.LastOrDefault() ?? "Estado actualizado.";

        Ultimo = resultado;

        foreach (var linea in resultado.Traza)
        {
            var texto = linea;

            foreach (var (curp, nombre) in Nombres)
                texto = texto.Replace(curp, nombre);

            Guardar(texto);
        }

        foreach (var alerta in resultado.Alertas)
            Guardar("ALERTA · " + alerta.Mensaje);

        int ocupados = Medicos.ObtenerTodos()
            .Count(m => m.Estado == EstadoMedico.Ocupado);

        Explicacion =
            $"{suceso}\n" +
            $"En espera: {Nucleo.Cola.Pacientes.Count}. " +
            $"En consulta: {ocupados}.";
    }
    private void Guardar(string texto)
    {
        Historial.Add($"[{Reloj.Ahora:HH:mm}] {texto}");
        if (Historial.Count > 1000) Historial.RemoveRange(0, Historial.Count - 1000);
    }
    private sealed class CarnetDemo : IServicioDeCarnet
    {
        public Dictionary<string, DatosDeCarnet> Datos { get; } = new();
        public DatosDeCarnet? Obtener(string curp) => Datos.GetValueOrDefault(curp);
    }
}

public sealed class EvaluadorModoDemo : IEvaluadorDeModo
{
    public ModoOperacion Evaluar(
        MedidasDeCarga medidas,
        ModoOperacion actual,
        DateTime ahora)
    {
        var afluencia = medidas.Afluencia < 0.30
            ? NivelAfluencia.Baja
            : medidas.Afluencia <= 0.70
                ? NivelAfluencia.Normal
                : NivelAfluencia.Excesiva;

        bool personalEscaso = medidas.Personal < 0.70;

        return ModoOperacionExtensiones.Combinar(
            personalEscaso, afluencia);
    }
}