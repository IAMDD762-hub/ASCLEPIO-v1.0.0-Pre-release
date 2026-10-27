using Asclepio.Escritorio;
using Asclepio.Core.Domain;
using Asclepio.Core.Engine;

int verificaciones = 0;
void Verificar(bool condicion, string texto)
{
    if (!condicion) throw new Exception(texto);
    Console.WriteLine("OK · " + texto); verificaciones++;
}
var demo = new SesionDemo();
demo.Siguiente();
Verificar(demo.Nombre(demo.Medicos.Obtener("M2")!.PacienteActual) == "Ana", "Paso 1: Ana va con Pérez por ventana suficiente");
demo.Siguiente();
Verificar(demo.Nucleo.Cola.Pacientes.Count == 1, "Paso 2: Bruno espera");
demo.Siguiente();
Verificar(demo.Nombre(demo.ColaOrdenada[0].Curp) == "Carla", "Paso 3: Carla se coloca antes que Bruno");
demo.Siguiente();
Verificar(demo.Nombre(demo.Medicos.Obtener("M1")!.PacienteActual) == "Diego", "Paso 4: el relleno asigna a Diego con Ruiz");
Verificar(demo.Ultimo.Traza.Any(t => t.Contains("Relleno")), "Paso 4: el motor confirma el relleno en su traza");
demo.Siguiente();
Verificar(demo.Medicos.Obtener("M1")!.Estado == EstadoMedico.Disponible && demo.Atendidos == 1, "Paso 5: Ruiz termina y no puede tomar una consulta larga");
demo.Siguiente();
Verificar(demo.Nombre(demo.Medicos.Obtener("M2")!.PacienteActual) == "Carla", "Paso 6: se respeta la prioridad al liberarse Pérez");
demo.Siguiente();
Verificar(demo.Nucleo.Motor.ModoActivo == ModoOperacion.M2Base, "Paso 7: el modo no cambia antes de sostener la carga");
demo.Siguiente();
Verificar(demo.Nucleo.Motor.ModoActivo == ModoOperacion.M3Pico && demo.Nombre(demo.Medicos.Obtener("M3")!.PacienteActual) == "Bruno", "Paso 8: M3 habilita al especialista y asigna a Bruno");
Verificar(demo.Atendidos == 2 && demo.Nucleo.Cola.Pacientes.Count == 0, "Recorrido: 2 terminados, 2 en consulta y cola vacía");

var manual = new SesionDemo();
manual.Agregar("Paciente", NivelGravedad.Verde, "Consulta larga");
manual.Avanzar(60);
Verificar(manual.Medicos.Obtener("M2")!.Estado == EstadoMedico.Ocupado, "Avanzar tiempo no termina consultas automáticamente");
var esperando = manual.Agregar("Espera", NivelGravedad.Amarillo, "Consulta larga");
manual.Cancelar(esperando);
Verificar(!manual.Nucleo.Cola.Contiene(esperando), "Retirar paciente elimina su entrada de la cola");
manual.QuitarBloque("M1");
var nuevo = manual.Agregar("Nueva", NivelGravedad.Verde, "Revisión general");
Verificar(manual.Medicos.Obtener("M1")!.PacienteActual == nuevo, "Cancelar el bloque vuelve a abrir la ventana");
var m6 = new SesionDemo();
m6.CambiarCarga(1.8, 0.5); m6.Avanzar(15);
Verificar(m6.Ultimo.Alertas.Any(a => a.Tipo == TipoDeAlerta.ModoCriticoPorConfirmar), "M6: el escenario expone la propuesta del motor");
m6.ConfirmarCritico();
Verificar(m6.Nucleo.Motor.ModoActivo == ModoOperacion.M6Critico, "M6: confirmar inmediatamente activa el modo");
Console.WriteLine($"\n{verificaciones} verificaciones de la demo aprobadas.");
