using System.ComponentModel.DataAnnotations;
using MiEconomia.Api.Cuentas;
using MiEconomia.Api.Domain;

namespace MiEconomia.Api.Transferencias;

public record GuardarTransferenciaRequest(
    [Required(ErrorMessage = "La cuenta de origen es obligatoria.")]
    Guid? CuentaOrigenId,

    [Required(ErrorMessage = "La cuenta de destino es obligatoria.")]
    Guid? CuentaDestinoId,

    [Required(ErrorMessage = "El monto es obligatorio.")]
    [Range(typeof(decimal), "0.01", "9999999999999999.99",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "El monto debe ser mayor a 0.")]
    decimal? Monto,

    [Required(ErrorMessage = "La fecha es obligatoria.")]
    DateOnly? Fecha,

    [StringLength(250, ErrorMessage = "La nota puede tener hasta 250 caracteres.")]
    string? Nota,

    // Las subcuentas son obligatorias solo si su cuenta tiene más de una.
    Guid? SubcuentaOrigenId = null,
    Guid? SubcuentaDestinoId = null,

    // Obligatorio solo si las subcuentas tienen distinta moneda: lo que llega al destino, en su moneda.
    [Range(typeof(decimal), "0.01", "9999999999999999.99",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "El monto que llega debe ser mayor a 0.")]
    decimal? MontoDestino = null);

/// <param name="Monto">Lo que sale del origen, en su moneda.</param>
/// <param name="MontoDestino">Lo que llega al destino, en su moneda. Igual a Monto si las monedas son iguales.</param>
public record TransferenciaResponse(
    Guid Id,
    decimal Monto,
    DateOnly Fecha,
    string? Nota,
    CuentaBasicaResponse Origen,
    CuentaBasicaResponse Destino,
    SubcuentaBasicaResponse SubcuentaOrigen,
    SubcuentaBasicaResponse SubcuentaDestino,
    decimal MontoDestino)
{
    public static TransferenciaResponse Desde(Transferencia t) =>
        new(t.Id, t.Monto, t.Fecha, t.Nota,
            CuentaBasicaResponse.Desde(t.CuentaOrigen), CuentaBasicaResponse.Desde(t.CuentaDestino),
            SubcuentaBasicaResponse.Desde(t.SubcuentaOrigen), SubcuentaBasicaResponse.Desde(t.SubcuentaDestino),
            t.MontoDestino ?? t.Monto);
}

/// <param name="CuentaId">Transferencias en las que participa la cuenta, como origen o como destino.</param>
/// <param name="SubcuentaId">Lo mismo, para una subcuenta.</param>
public record TransferenciasFiltro(
    DateOnly? Desde,
    DateOnly? Hasta,
    Guid? CuentaId,
    Guid? SubcuentaId,
    [Range(1, int.MaxValue, ErrorMessage = "La página empieza en 1.")]
    int Pagina = 1,
    [Range(1, 200, ErrorMessage = "El tamaño de página debe estar entre 1 y 200.")]
    int Tamanio = 50);
