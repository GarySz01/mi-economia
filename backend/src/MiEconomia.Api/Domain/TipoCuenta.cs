namespace MiEconomia.Api.Domain;

/// <summary>Solo sirve para mostrar y agrupar; el saldo se calcula igual para todos los tipos.</summary>
public enum TipoCuenta
{
    Efectivo,
    CuentaBancaria,
    TarjetaCredito,
    BilleteraVirtual,
    Inversion,
    Otra
}
