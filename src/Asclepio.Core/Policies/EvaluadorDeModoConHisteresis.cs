using Asclepio.Core.Abstractions;
using Asclepio.Core.Domain;

namespace Asclepio.Core.Policies;

/// <summary>Umbrales de ejemplo (nota *13); se calibran por unidad, día y franja horaria.</summary>
public sealed record OpcionesDeModo(
    double UmbralAfluenciaBaja = 0.7,
    double UmbralAfluenciaAlta = 1.3,
    double UmbralPersonal = 0.85,
    double Histeresis = 0.10,
    TimeSpan? Sostenido = null)
{
    public TimeSpan TiempoSostenido => Sostenido ?? TimeSpan.FromMinutes(15);
}

/// <summary>
/// Dos defensas contra la oscilación: (1) el umbral de salida es distinto del de entrada (histéresis de 10 %),
/// (2) el nuevo modo debe sostenerse varios minutos antes de adoptarse.
/// </summary>
public sealed class EvaluadorDeModoConHisteresis : IEvaluadorDeModo
{
    private readonly OpcionesDeModo _o;
    private ModoOperacion? _candidato;
    private DateTime _candidatoDesde;

    public EvaluadorDeModoConHisteresis(OpcionesDeModo? opciones = null) => _o = opciones ?? new OpcionesDeModo();

    public ModoOperacion Evaluar(MedidasDeCarga medidas, ModoOperacion actual, DateTime ahora)
    {
        var escaso = ClasificarPersonal(medidas.Personal, actual.PersonalEscaso());
        var afluencia = ClasificarAfluencia(medidas.Afluencia, actual.Afluencia());
        var objetivo = ModoOperacionExtensiones.Combinar(escaso, afluencia);

        if (objetivo == actual)
        {
            _candidato = null;
            return actual;
        }

        if (_candidato != objetivo)
        {
            _candidato = objetivo;
            _candidatoDesde = ahora;
        }

        if (ahora - _candidatoDesde >= _o.TiempoSostenido)
        {
            _candidato = null;
            return objetivo;
        }

        return actual;
    }

    private bool ClasificarPersonal(double personal, bool actualEscaso)
    {
        var u = _o.UmbralPersonal;
        return actualEscaso
            ? !(personal > u * (1 + _o.Histeresis))   // sigue escaso salvo que suba claramente
            : personal < u * (1 - _o.Histeresis);     // pasa a escaso solo si baja claramente
    }

    private NivelAfluencia ClasificarAfluencia(double a, NivelAfluencia actual)
    {
        var bajo = _o.UmbralAfluenciaBaja;
        var alto = _o.UmbralAfluenciaAlta;
        var h = _o.Histeresis;

        switch (actual)
        {
            case NivelAfluencia.Baja:
                if (a > alto * (1 + h)) return NivelAfluencia.Excesiva;
                return a > bajo * (1 + h) ? NivelAfluencia.Normal : NivelAfluencia.Baja;

            case NivelAfluencia.Excesiva:
                if (a < bajo * (1 - h)) return NivelAfluencia.Baja;
                return a < alto * (1 - h) ? NivelAfluencia.Normal : NivelAfluencia.Excesiva;

            default: // Normal
                if (a < bajo * (1 - h)) return NivelAfluencia.Baja;
                return a > alto * (1 + h) ? NivelAfluencia.Excesiva : NivelAfluencia.Normal;
        }
    }
}
