namespace Asclepio;

// CarneCURP unificado
//Muchos atributos están inicializados en "cero" para que el compilador no se ponga de mamón. XD
public class CarneCURP
{
    public string Curp { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string TipoSangre { get; set; } = string.Empty;

    //Como bien dijiste, el CURP no lo prueba todo, así que comprobamos con los siguientes datos:
    public DateOnly FechaNacimiento { get; set; }

    public Direccion Residencia {get; set;} = new Direccion();

    public int Edad {get; set;}
    //Estos atributos por si acaso:
    public long Numtel {get; set;}
    public string Folio {get; set;} = string.Empty;

    public int CalcularEdad()
    {
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        int edad = hoy.Year - FechaNacimiento.Year;
        
        // Si aún no cumple años en el año actual, le restamos 1
        if (hoy < FechaNacimiento.AddYears(edad)) 
        {
            edad--;
        }
        
        return edad;
    }
    
    // Listas que guardan el historial completo del paciente
    public List<CitaConsulta> HistorialConsultas { get; set; } = new List<CitaConsulta>();
    public List<RegistroUrgencia> HistorialUrgencias { get; set; } = new List<RegistroUrgencia>();

    //Método constructor vacío por si acaso.
    public CarneCURP()
    { 
    }
    public CarneCURP(string curp, string nombreCompleto, string tipoSangre, 
    DateOnly fechaNacimiento, Direccion residencia, long numtel, string folio)
    {
        Curp = curp;
        NombreCompleto = nombreCompleto;
        TipoSangre = tipoSangre;
        FechaNacimiento = fechaNacimiento;
        Residencia = residencia;
        Edad = CalcularEdad();
        Numtel = numtel;
        Folio = folio;
        Console.WriteLine("CarneCURP creado.");
        
        // Es vital inicializar las listas vacías en el constructor 
        // para evitar excepciones de nulos al intentar agregar datos después
        HistorialConsultas = new List<CitaConsulta>();
        HistorialUrgencias = new List<RegistroUrgencia>();
    }
}

//CLase aparte para la dirección del paciente:
public class Direccion
{
    public string callenumero {get; set;} = "";
    public string colonia {get; set;} = "";
    public string alcaldiamunicipio {get; set;} = "";
    public string codpost {get; set;} = "";
}

// 2. Clase para las citas programadas
public class CitaConsulta
{
    public DateTime Fecha { get; set; }
    public string MedicoAsignado { get; set; } = "";
    public string Consultorio { get; set; } = "";
}

// 3. Clase para las emergencias:
public class RegistroUrgencia
{
    public DateTime FechaIngreso { get; set; }
    public int NivelTriage { get; set; } // Del 1 al 5; 1 = prioridad máxima, 5 = menor prioridad, tal como en la guía
    public string Sintomas { get; set; } = "";
}