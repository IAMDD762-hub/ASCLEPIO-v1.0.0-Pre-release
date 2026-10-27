using Asclepio.Core.Abstractions;
using Asclepio.Core.Composition;
using Asclepio.Core.Domain;
using Asclepio.Core.Engine;
using Asclepio.Core.Infrastructure;

// Demostración del motor en consola, con reloj simulado: una "mañana" comprimida en segundos.
// No es la interfaz final; sirve para ver las decisiones del motor (la traza) antes de construir la ventana.

var inicio = new DateTime(2026, 10, 26, 8, 0, 0);
var reloj = new RelojManual(inicio);

var medicos = new RepositorioDeMedicosEnMemoria(new[]
{
    new Medico("M1", "Dra. Ruiz",  RolMedico.General),
    new Medico("M2", "Dr. Pérez",  RolMedico.General),
    new Medico("M3", "Dra. Soto",  RolMedico.Especialidad, esEspecialista: true),
});

// M1 tiene una cita fija a las 08:40: solo puede atender consultas que terminen antes (ventana de 40 min).
medicos.Obtener("M1")!.ProximoBloqueFijo = inicio.AddMinutes(40);

var carnet = new ServicioDeCarnetSimulado();
var medidas = new ProveedorDeMedidasFijo();
var nucleo = ConstructorDelNucleo.CrearPorDefecto(reloj, medicos, carnet, medidas, new NotificadorConsola());

void Mostrar(string titulo, ResultadoDelCiclo r)
{
    Console.WriteLine($"\n=== {reloj.Ahora:HH:mm} · {titulo} · modo {r.Modo} ===");
    foreach (var linea in r.Traza) Console.WriteLine("  " + linea);
    foreach (var (curp, rango) in r.Esperas)
        Console.WriteLine($"  espera {curp}: {rango.P50.TotalMinutes:0}–{rango.P80.TotalMinutes:0} min");
}

string Curp(int n) => $"PEGL900101HDFRRN{n:00}"; // CURP de ejemplo, 18 caracteres

Mostrar("llegan 4 pacientes", Registrar(1, NivelGravedad.Verde, "revisión general"));
Mostrar("", Registrar(2, NivelGravedad.Amarillo, "dolor abdominal"));
Mostrar("", Registrar(3, NivelGravedad.Azul, "receta de continuidad"));
Mostrar("", Registrar(4, NivelGravedad.Verde, "revisión general"));

reloj.Avanzar(TimeSpan.FromMinutes(18));
Mostrar("Dra. Ruiz termina su consulta", nucleo.Consulta.FinalizarConsulta("M1"));

reloj.Avanzar(TimeSpan.FromMinutes(5));
Mostrar("sube la afluencia (modo)", CambiarMedidas(afluencia: 1.8, personal: 0.6));

reloj.Avanzar(TimeSpan.FromMinutes(20));
Mostrar("sigue alta 20 min después", nucleo.Consulta.Refrescar());

ResultadoDelCiclo Registrar(int n, NivelGravedad g, string motivo)
    => nucleo.Consulta.RegistrarLlegada(Curp(n), g, motivo);

ResultadoDelCiclo CambiarMedidas(double afluencia, double personal)
{
    medidas.Afluencia = afluencia;
    medidas.Personal = personal;
    return nucleo.Consulta.Refrescar();
}

/// <summary>Imprime los avisos como lo haría la interfaz (aquí en la consola).</summary>
sealed class NotificadorConsola : INotificador
{
    public void AsignacionRealizada(Asignacion a) { }
    public void EsperasActualizadas(IReadOnlyDictionary<string, RangoDeEspera> e) { }
    public void AlertaEmitida(Alerta a) => Console.WriteLine($"  ⚠ {a.Tipo}: {a.Mensaje}");
}
