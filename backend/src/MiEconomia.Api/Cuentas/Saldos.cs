using Microsoft.EntityFrameworkCore;
using MiEconomia.Api.Data;
using MiEconomia.Api.Domain;

namespace MiEconomia.Api.Cuentas;

public static class Saldos
{
    /// <summary>
    /// Cuánto cambió el saldo de cada subcuenta del usuario desde el saldo inicial: ingresos − gastos
    /// + transferencias recibidas − enviadas, cada una en la moneda de su subcuenta. Las sumas se hacen
    /// en la base; las subcuentas sin movimientos no aparecen en el diccionario.
    /// </summary>
    /// <param name="cuentaId">Si se pasa, solo las subcuentas de esa cuenta.</param>
    public static async Task<Dictionary<Guid, decimal>> MovimientosNetosAsync(
        AppDbContext db, Guid usuarioId, Guid? cuentaId = null)
    {
        var movimientos = db.Movimientos.Where(m => m.UsuarioId == usuarioId);
        var enviadas = db.Transferencias.Where(t => t.UsuarioId == usuarioId);
        var recibidas = enviadas;
        if (cuentaId is not null)
        {
            movimientos = movimientos.Where(m => m.CuentaId == cuentaId);
            enviadas = enviadas.Where(t => t.CuentaOrigenId == cuentaId);
            recibidas = recibidas.Where(t => t.CuentaDestinoId == cuentaId);
        }

        var porTipo = await movimientos
            .GroupBy(m => new { m.SubcuentaId, m.Categoria.Tipo })
            .Select(g => new { g.Key.SubcuentaId, g.Key.Tipo, Total = g.Sum(m => m.Monto) })
            .ToListAsync();
        var salidas = await enviadas
            .GroupBy(t => t.SubcuentaOrigenId)
            .Select(g => new { SubcuentaId = g.Key, Total = g.Sum(t => t.Monto) })
            .ToListAsync();
        // Si las monedas son distintas, al destino llega MontoDestino.
        var entradas = await recibidas
            .GroupBy(t => t.SubcuentaDestinoId)
            .Select(g => new { SubcuentaId = g.Key, Total = g.Sum(t => t.MontoDestino ?? t.Monto) })
            .ToListAsync();

        var netos = new Dictionary<Guid, decimal>();
        void Sumar(Guid id, decimal valor) => netos[id] = netos.GetValueOrDefault(id) + valor;

        foreach (var m in porTipo)
            Sumar(m.SubcuentaId, m.Tipo == TipoMovimiento.Ingreso ? m.Total : -m.Total);
        foreach (var t in salidas)
            Sumar(t.SubcuentaId, -t.Total);
        foreach (var t in entradas)
            Sumar(t.SubcuentaId, t.Total);

        return netos;
    }
}
