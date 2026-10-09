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
    string? Nota);

public record TransferenciaResponse(
    Guid Id,
    decimal Monto,
    DateOnly Fecha,
    string? Nota,
    CuentaBasicaResponse Origen,
    CuentaBasicaResponse Destino)
{
    public static TransferenciaResponse Desde(Transferencia t) =>
        new(t.Id, t.Monto, t.Fecha, t.Nota,
            CuentaBasicaResponse.Desde(t.CuentaOrigen), CuentaBasicaResponse.Desde(t.CuentaDestino));
}

/// <param name="CuentaId">Transferencias en las que participa la cuenta, como origen o como destino.</param>
public record TransferenciasFiltro(
    DateOnly? Desde,
    DateOnly? Hasta,
    Guid? CuentaId,
    [Range(1, int.MaxValue, ErrorMessage = "La página empieza en 1.")]
    int Pagina = 1,
    [Range(1, 200, ErrorMessage = "El tamaño de página debe estar entre 1 y 200.")]
    int Tamanio = 50);
