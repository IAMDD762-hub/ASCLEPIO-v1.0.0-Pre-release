using Asclepio.Core.Composition;
using Asclepio.Core.Domain;
using Asclepio.Core.Engine;
using Asclepio.Core.Infrastructure;
using Asclepio.Core.Policies;

namespace Asclepio.Pruebas;

/// <summary>Verificaciones mínimas sin paquetes externos.</summary>
static class Verifica
{
    public static void Cierto(bool condicion, string mensaje)
    {
        if (!condicion) throw new Exception("Falló: " + mensaje);
    }

    public static void Igual<T>(T esperado, T real, string mensaje)
    {
        if (!EqualityComparer<T>.Default.Equals(esperado, real))
            throw new Exception($"Falló: {mensaje} (esperado {esperado}, real {real})");
    }
}

/// <summary>Un escenario completo con reloj manual, para que cada prueba arme lo que necesita.</summary>
sealed class Escenario
{
    public static readonly DateTime Inicio = new(2026, 10, 26, 8, 0, 0);

    public RelojManual Reloj { get; } = new(Inicio);
    public RepositorioDeMedicosEnMemoria Medicos { get; } = new();
    public ProveedorDeMedidasFijo Medidas { get; } = new();
    public NucleoAsclepio Nucleo { get; private set; } = null!;

    public Escenario(IEnumerable<Medico> medicos, EstimadorDeDuracionPorMotivo? duraciones = null)
    {
        foreach (var m in medicos) Medicos.Agregar(m);
        Nucleo = ConstructorDelNucleo.CrearPorDefecto(
            Reloj, Medicos, new ServicioDeCarnetSimulado(), Medidas, duraciones: duraciones);
    }

    public static string Curp(int n) => $"PEGL900101HDFRRN{n:00}";

    public ResultadoDelCiclo Llega(int n, NivelGravedad g, string motivo = "revisión")
        => Nucleo.Consulta.RegistrarLlegada(Curp(n), g, motivo);
}

static class Program
{
    static int Main()
    {
        var pruebas = new (string Nombre, Action Cuerpo)[]
        {
            ("Mejor ajuste: gana la ventana más pequeña que alcanza", MejorAjuste),
            ("Ventana insuficiente: no se asigna a quien no cabe antes de la cita", VentanaInsuficiente),
            ("Prioridad: amarillo antes que verde al liberarse un médico", PrioridadPorGravedad),
            ("Relleno: el que sí cabe ocupa el hueco que el primero no puede usar", Relleno),
            ("Fin de consulta libera al médico y asigna al siguiente", FinDeConsultaAsignaSiguiente),
            ("Especialista como general solo cuando el modo (Pico) lo permite", EspecialistaSoloEnPico),
            ("Histéresis: un valor cercano al umbral no cambia el modo", Histeresis),
            ("Modo: el cambio exige sostenerse el tiempo configurado", ModoSostenido),
            ("Modo Crítico: se propone, no se aplica hasta confirmar", ModoCriticoRequiereConfirmacion),
            ("Alerta de cobertura: cola sin médicos", ColaSinMedicos),
            ("Estimador de duración aprende de tiempos reales", EstimadorAprende),
            ("CURP inválido se rechaza", CurpInvalido),
        };

        var fallos = 0;
        foreach (var (nombre, cuerpo) in pruebas)
        {
            try
            {
                cuerpo();
                Console.WriteLine($"  OK    {nombre}");
            }
            catch (Exception e)
            {
                fallos++;
                Console.WriteLine($"  FALLA {nombre}\n        {e.Message}");
            }
        }

        Console.WriteLine(fallos == 0 ? "\nTodas las pruebas pasaron." : $"\n{fallos} prueba(s) fallaron.");
        return fallos == 0 ? 0 : 1;
    }

    static void MejorAjuste()
    {
        var ventanaCorta = new Medico("M1", "Con cita a la 1 h", RolMedico.General);
        var sinCita = new Medico("M2", "Sin cita", RolMedico.General);
        ventanaCorta.ProximoBloqueFijo = Escenario.Inicio.AddMinutes(60);

        var e = new Escenario(new[] { ventanaCorta, sinCita });
        e.Llega(1, NivelGravedad.Verde);

        Verifica.Igual(EstadoMedico.Ocupado, ventanaCorta.Estado, "debe usar la ventana más pequeña que alcance");
        Verifica.Igual(EstadoMedico.Disponible, sinCita.Estado, "deja libre al médico sin límite para casos largos");
    }

    static void VentanaInsuficiente()
    {
        var medico = new Medico("M1", "Cita en 10 min", RolMedico.General);
        medico.ProximoBloqueFijo = Escenario.Inicio.AddMinutes(10);

        var e = new Escenario(new[] { medico });
        e.Llega(1, NivelGravedad.Verde);

        Verifica.Igual(EstadoMedico.Disponible, medico.Estado, "no cabe una consulta antes de su cita");
        Verifica.Igual(1, e.Nucleo.Cola.Pacientes.Count, "el paciente sigue esperando");
    }

    static void PrioridadPorGravedad()
    {
        var medico = new Medico("M1", "Único", RolMedico.General);
        var e = new Escenario(new[] { medico });

        e.Llega(1, NivelGravedad.Verde);     // lo toma de inmediato
        e.Llega(2, NivelGravedad.Verde);     // espera
        e.Llega(3, NivelGravedad.Amarillo);  // espera, pero más grave

        e.Reloj.Avanzar(TimeSpan.FromMinutes(10));
        e.Nucleo.Consulta.FinalizarConsulta("M1");

        Verifica.Igual(Escenario.Curp(3), medico.PacienteActual, "el amarillo pasa antes que el verde");
    }

    static void Relleno()
    {
        var duraciones = new EstimadorDeDuracionPorMotivo();
        for (var i = 0; i < 5; i++)
        {
            duraciones.RegistrarReal("dolor abdominal", TimeSpan.FromMinutes(40));
            duraciones.RegistrarReal("receta", TimeSpan.FromMinutes(4));
        }

        var medico = new Medico("M1", "Cita en 20 min", RolMedico.General);
        medico.ProximoBloqueFijo = Escenario.Inicio.AddMinutes(20);

        var e = new Escenario(new[] { medico }, duraciones);
        e.Llega(1, NivelGravedad.Amarillo, "dolor abdominal");   // 40 min: no cabe
        e.Llega(2, NivelGravedad.Azul, "receta");                // 4 min: sí cabe

        Verifica.Igual(Escenario.Curp(2), medico.PacienteActual, "el paciente corto rellena el hueco");
        Verifica.Cierto(e.Nucleo.Cola.Contiene(Escenario.Curp(1)), "el primero sigue esperando su ventana");
    }

    static void FinDeConsultaAsignaSiguiente()
    {
        var medico = new Medico("M1", "Único", RolMedico.General);
        var e = new Escenario(new[] { medico });

        e.Llega(1, NivelGravedad.Verde);
        e.Llega(2, NivelGravedad.Verde);
        Verifica.Igual(Escenario.Curp(1), medico.PacienteActual, "atiende al primero");

        e.Reloj.Avanzar(TimeSpan.FromMinutes(12));
        var r = e.Nucleo.Consulta.FinalizarConsulta("M1");

        Verifica.Igual(Escenario.Curp(2), medico.PacienteActual, "pasa al siguiente");
        Verifica.Igual(1, r.Asignaciones.Count, "el ciclo reporta la asignación");
    }

    static void EspecialistaSoloEnPico()
    {
        var especialista = new Medico("S1", "Especialista", RolMedico.Especialidad, esEspecialista: true);
        var e = new Escenario(new[] { especialista });

        e.Llega(1, NivelGravedad.Verde);
        Verifica.Igual(EstadoMedico.Disponible, especialista.Estado, "en modo Base no atiende consulta general");

        e.Medidas.Afluencia = 1.8; // pasa a Pico, pero debe sostenerse
        e.Nucleo.Consulta.Refrescar();
        e.Reloj.Avanzar(TimeSpan.FromMinutes(15));
        e.Nucleo.Consulta.Refrescar();

        Verifica.Igual(ModoOperacion.M3Pico, e.Nucleo.Motor.ModoActivo, "modo Pico adoptado");
        Verifica.Igual(EstadoMedico.Ocupado, especialista.Estado, "en Pico el especialista atiende");
    }

    static void Histeresis()
    {
        var evaluador = new EvaluadorDeModoConHisteresis(new OpcionesDeModo(Sostenido: TimeSpan.Zero));

        // Desde Valle (afluencia baja), 0.72 está apenas arriba de 0.7 pero dentro de la banda de histéresis.
        var cerca = evaluador.Evaluar(new MedidasDeCarga(0.72, 1.0), ModoOperacion.M1Valle, Escenario.Inicio);
        Verifica.Igual(ModoOperacion.M1Valle, cerca, "sigue en Valle");

        // Claramente por encima del umbral de salida: sí cambia.
        var lejos = evaluador.Evaluar(new MedidasDeCarga(0.9, 1.0), ModoOperacion.M1Valle, Escenario.Inicio);
        Verifica.Igual(ModoOperacion.M2Base, lejos, "pasa a Base");
    }

    static void ModoSostenido()
    {
        var evaluador = new EvaluadorDeModoConHisteresis();
        var t = Escenario.Inicio;
        var alta = new MedidasDeCarga(1.8, 1.0);

        Verifica.Igual(ModoOperacion.M2Base, evaluador.Evaluar(alta, ModoOperacion.M2Base, t), "aún no");
        Verifica.Igual(ModoOperacion.M2Base, evaluador.Evaluar(alta, ModoOperacion.M2Base, t.AddMinutes(14)), "14 min no bastan");
        Verifica.Igual(ModoOperacion.M3Pico, evaluador.Evaluar(alta, ModoOperacion.M2Base, t.AddMinutes(15)), "15 min sí");
    }

    static void ModoCriticoRequiereConfirmacion()
    {
        var e = new Escenario(new[] { new Medico("M1", "Único", RolMedico.General) });
        e.Medidas.Afluencia = 1.8;
        e.Medidas.Personal = 0.5;

        e.Nucleo.Consulta.Refrescar();
        e.Reloj.Avanzar(TimeSpan.FromMinutes(15));
        var r = e.Nucleo.Consulta.Refrescar();

        Verifica.Igual(ModoOperacion.M2Base, e.Nucleo.Motor.ModoActivo, "no se aplica sin confirmación");
        Verifica.Cierto(r.Alertas.Any(a => a.Tipo == TipoDeAlerta.ModoCriticoPorConfirmar), "pide confirmación");

        e.Nucleo.Consulta.ConfirmarModoCritico();
        Verifica.Igual(ModoOperacion.M6Critico, e.Nucleo.Motor.ModoActivo, "tras confirmar, queda en Crítico");
    }

    static void ColaSinMedicos()
    {
        var medico = new Medico("M1", "Fuera de servicio", RolMedico.General);
        medico.CambiarEstado(EstadoMedico.FueraDeServicio);

        var e = new Escenario(new[] { medico });
        var r = e.Llega(1, NivelGravedad.Verde);

        Verifica.Cierto(r.Alertas.Any(a => a.Tipo == TipoDeAlerta.ColaSinMedicos), "alerta de cobertura");
    }

    static void EstimadorAprende()
    {
        var estimador = new EstimadorDeDuracionPorMotivo();
        var paciente = new Paciente(Escenario.Curp(1), NivelGravedad.Verde, ClasePaciente.AgudoMayor, "control", Escenario.Inicio);

        Verifica.Igual(TimeSpan.FromMinutes(15), estimador.Mediana(paciente), "valor base sin datos");

        for (var i = 0; i < 5; i++) estimador.RegistrarReal("control", TimeSpan.FromMinutes(10));

        Verifica.Igual(TimeSpan.FromMinutes(10), estimador.Mediana(paciente), "aprende con 5 muestras");
    }

    static void CurpInvalido()
    {
        var e = new Escenario(new[] { new Medico("M1", "Único", RolMedico.General) });
        var rechazado = false;
        try { e.Nucleo.Consulta.RegistrarLlegada("CORTO", NivelGravedad.Verde, "revisión"); }
        catch (ArgumentException) { rechazado = true; }

        Verifica.Cierto(rechazado, "debe lanzar ArgumentException");
    }
}
