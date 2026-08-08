namespace GestionGastos.Api.Comun;

/// <summary>
/// El rango de fechas que comparten el listado (RF-18) y el dashboard (RF-21). Vive aparte
/// para que los dos resuelvan el default igual: si divergieran, el resumen del mes y el
/// listado mostrarian periodos distintos.
/// </summary>
public static class Periodo
{
    /// <summary>
    /// AC-25: sin fechas, el mes actual. Si viene solo un extremo, el otro se completa con
    /// el borde del mes de ese extremo, para no dejar el rango abierto.
    /// </summary>
    public static (DateOnly Inicio, DateOnly Fin) PedidoOMesActual(DateOnly? desde, DateOnly? hasta)
    {
        var referencia = desde ?? hasta ?? DateOnly.FromDateTime(DateTime.Now);
        var primeroDelMes = new DateOnly(referencia.Year, referencia.Month, 1);

        return (
            desde ?? primeroDelMes,
            hasta ?? primeroDelMes.AddMonths(1).AddDays(-1));
    }
}
