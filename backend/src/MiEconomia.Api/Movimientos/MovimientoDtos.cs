using System.ComponentModel.DataAnnotations;
using MiEconomia.Api.Categorias;
using MiEconomia.Api.Cuentas;
using MiEconomia.Api.Domain;

namespace MiEconomia.Api.Movimientos;

public record GuardarMovimientoRequest(
    [Required(ErrorMessage = "La categoría es obligatoria.")]
    Guid? CategoriaId,

    [Required(ErrorMessage = "La cuenta es obligatoria.")]
    Guid? CuentaId,

    [Required(ErrorMessage = "El monto es obligatorio.")]
    [Range(typeof(decimal), "0.01", "9999999999999999.99",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "El monto debe ser mayor a 0.")]
    decimal? Monto,

    [Required(ErrorMessage = "La fecha es obligatoria.")]
    DateOnly? Fecha,

    [StringLength(250, ErrorMessage = "La nota puede tener hasta 250 caracteres.")]
    string? Nota);

public record MovimientoResponse(
    Guid Id,
    decimal Monto,
    DateOnly Fecha,
    string? Nota,
    CategoriaResponse Categoria,
    CuentaBasicaResponse Cuenta)
{
    public static MovimientoResponse Desde(Movimiento m) =>
        new(m.Id, m.Monto, m.Fecha, m.Nota, CategoriaResponse.Desde(m.Categoria), CuentaBasicaResponse.Desde(m.Cuenta));
}

public record MovimientosFiltro(
    DateOnly? Desde,
    DateOnly? Hasta,
    Guid? CategoriaId,
    Guid? CuentaId,
    TipoMovimiento? Tipo,
    [Range(1, int.MaxValue, ErrorMessage = "La página empieza en 1.")]
    int Pagina = 1,
    [Range(1, 200, ErrorMessage = "El tamaño de página debe estar entre 1 y 200.")]
    int Tamanio = 50);

public record PaginaResponse<T>(IReadOnlyList<T> Items, int Total, int Pagina, int Tamanio);
