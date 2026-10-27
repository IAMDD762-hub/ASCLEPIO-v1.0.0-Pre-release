using Asclepio;
using System.Text.Json;
int n=0;
void Ok(bool b,string caso){n++;if(!b)throw new Exception("FALLO: "+caso);}
void Rechaza(Action accion,string caso){bool rechazo=false;try{accion();}catch(ArgumentException){rechazo=true;}catch(JsonException){rechazo=true;}Ok(rechazo,caso);}
CarneCURP Ejemplo()=>new(){Curp="PEAL000101HDFRRN09",NombreCompleto="Paciente ficticio",FechaNacimiento=new(2000,1,1),Numtel="+52 5500000000",Residencia=new(){codpost="01234"}};
string dir=Path.Combine(Path.GetTempPath(),"asclepio-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
try{
 var p=Ejemplo();Ok(p.CalcularEdad(new(2026,1,1))==26,"cumpleaños");Ok(p.CalcularEdad(new(2025,12,31))==25,"antes del cumpleaños");
 var b=Ejemplo();b.FechaNacimiento=new(2000,2,29);Ok(b.CalcularEdad(new(2025,2,28))==25,"29 febrero en año no bisiesto");Ok(b.CalcularEdad(new(2024,2,28))==23,"29 febrero en año bisiesto");
 p.HistorialConsultas.Add(new(){Fecha=DateTime.Today.AddDays(2),MedicoAsignado="Dra. Ejemplo",Consultorio="01"});p.HistorialUrgencias.Add(new(){FechaIngreso=DateTime.Today.AddDays(-1),NivelTriage=2,Sintomas="Ejemplo ficticio"});
 Validacion.Carnet(p);n++;
 string db=Path.Combine(dir,"pacientes.json");var repo=new RepositorioCarnets(db);repo.Guardar(p,null);Ok(repo.Pacientes.Count==1,"guardar nuevo");
 var recarga=new RepositorioCarnets(db);Ok(recarga.Pacientes.Single().Numtel.StartsWith("+52"),"teléfono texto");Ok(recarga.Pacientes.Single().Residencia.codpost=="01234","CP ceros");
 Ok(recarga.Pacientes.Single().HistorialConsultas.Single().Id==p.HistorialConsultas[0].Id,"ID consulta persistente");Ok(recarga.Pacientes.Single().HistorialUrgencias.Single().NivelTriage==2,"historial urgencias");
 string antes=File.ReadAllText(db);Rechaza(()=>repo.Guardar(Ejemplo(),null),"duplicado CURP");Ok(File.ReadAllText(db)==antes,"rechazo no altera disco");
 p.NombreCompleto="Nombre actualizado";repo.Guardar(p,p.Curp);Ok(new RepositorioCarnets(db).Pacientes.Single().NombreCompleto==p.NombreCompleto,"actualizar");Ok(File.ReadAllText(db+".bak")==antes,"backup anterior");
 var externo=repo.Pacientes.Single();externo.NombreCompleto="Mutación externa";Ok(repo.Pacientes.Single().NombreCompleto==p.NombreCompleto,"repositorio aislado");
 string archivo=Path.Combine(dir,"carnet.json");Intercambio.Exportar(archivo,p);var importado=Intercambio.Importar(archivo);Ok(importado.Folio==p.Folio,"folio ida vuelta");Ok(importado.HistorialConsultas.Single().Id==p.HistorialConsultas[0].Id,"consulta ida vuelta");Ok(importado.HistorialUrgencias.Single().Sintomas==p.HistorialUrgencias[0].Sintomas,"urgencia ida vuelta");Ok(!File.ReadAllText(archivo).Contains("\"edad\""),"edad no persistida");
 foreach(Action<CarneCURP> cambio in new Action<CarneCURP>[] {x=>x.Curp="xxx",x=>x.NombreCompleto="",x=>x.FechaNacimiento=DateOnly.FromDateTime(DateTime.Today.AddDays(1)),x=>x.TipoSangre="X",x=>x.Residencia.codpost="123",x=>x.Numtel="abc",x=>x.HistorialConsultas=null!,x=>x.Residencia=null!}){var malo=Ejemplo();cambio(malo);Rechaza(()=>Validacion.Carnet(malo),"validación campos");}
 for(int i=1;i<=5;i++){Validacion.Urgencia(new(){NivelTriage=i,Sintomas="Ejemplo"});n++;}
 Rechaza(()=>Validacion.Urgencia(new(){NivelTriage=0,Sintomas="Ejemplo"}),"triage bajo");Rechaza(()=>Validacion.Urgencia(new(){NivelTriage=6,Sintomas="Ejemplo"}),"triage alto");Rechaza(()=>Validacion.Urgencia(new(){FechaIngreso=DateTime.Now.AddDays(1),Sintomas="Ejemplo"}),"ingreso futuro");
 Rechaza(()=>Validacion.Consulta(new(){MedicoAsignado="",Consultorio="01"}),"médico vacío");
 var doble=Intercambio.Copiar(p);doble.HistorialConsultas.Add(doble.HistorialConsultas[0]);Rechaza(()=>Validacion.Carnet(doble),"ID duplicado");
 var original=File.ReadAllText(archivo);File.WriteAllText(archivo,original.Replace("\"version\": 1","\"version\": 99"));Rechaza(()=>Intercambio.Importar(archivo),"versión desconocida");
 File.WriteAllText(archivo,"{}");Rechaza(()=>Intercambio.Importar(archivo),"paquete vacío");File.WriteAllText(archivo,"{roto");Rechaza(()=>Intercambio.Importar(archivo),"JSON malformado");
 File.WriteAllText(Path.Combine(dir,"roto.json"),"{}");Rechaza(()=>new RepositorioCarnets(Path.Combine(dir,"roto.json")),"base vacía rechazada");
 File.WriteAllText(archivo,new string(' ',5*1024*1024+1));Rechaza(()=>Intercambio.Importar(archivo),"límite tamaño");
 var segundo=Ejemplo();segundo.Curp="PEAL000101HDFRRN10";repo.Guardar(segundo,null);var editar=Intercambio.Copiar(p);editar.Curp=segundo.Curp;Rechaza(()=>repo.Guardar(editar,p.Curp),"editar no pisa otro paciente");Ok(repo.Pacientes.Count==2,"pacientes preservados");
 Console.WriteLine($"OK: {n} comprobaciones de modelos, persistencia e intercambio.");
}finally{Directory.Delete(dir,true);}
