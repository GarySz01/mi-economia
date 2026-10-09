using Microsoft.EntityFrameworkCore;
using MiEconomia.Api.Data;
using MiEconomia.Api.Domain;

namespace MiEconomia.Api.Cuentas;

public static class Saldos
{
    /// <summary>
    /// Cuánto cambió el saldo de cada cuenta del usuario desde el saldo inicial: ingresos − gastos
    /// + transferencias recibidas − enviadas. Las sumas se hacen en la base; las cuentas sin
    /// movimientos no aparecen en el diccionario.
    /// </summary>
    public static async Task<Dictionary<Guid, decimal>> MovimientosNetosAsync(
        AppDbContext db, Guid usuarioId, Guid? cuentaId = null)
    {
        var movimientos = db.Movimientos.Where(m => m.UsuarioId == usuarioId);
        var transferencias = db.Transferencias.Where(t => t.UsuarioId == usuarioId);
        if (cuentaId is not null)
        {
            movimientos = movimientos.Where(m => m.CuentaId == cuentaId);
            transferencias = transferencias.Where(t => t.CuentaOrigenId == cuentaId || t.CuentaDestinoId == cuentaId);
        }

        var porTipo = await movimientos
            .GroupBy(m => new { m.CuentaId, m.Categoria.Tipo })
            .Select(g => new { g.Key.CuentaId, g.Key.Tipo, Total = g.Sum(m => m.Monto) })
            .ToListAsync();
        var enviadas = await transferencias
            .GroupBy(t => t.CuentaOrigenId)
            .Select(g => new { CuentaId = g.Key, Total = g.Sum(t => t.Monto) })
            .ToListAsync();
        var recibidas = await transferencias
            .GroupBy(t => t.CuentaDestinoId)
            .Select(g => new { CuentaId = g.Key, Total = g.Sum(t => t.Monto) })
            .ToListAsync();

        var netos = new Dictionary<Guid, decimal>();
        void Sumar(Guid id, decimal valor) => netos[id] = netos.GetValueOrDefault(id) + valor;

        foreach (var m in porTipo)
            Sumar(m.CuentaId, m.Tipo == TipoMovimiento.Ingreso ? m.Total : -m.Total);
        foreach (var t in enviadas)
            Sumar(t.CuentaId, -t.Total);
        foreach (var t in recibidas)
            Sumar(t.CuentaId, t.Total);

        return netos;
    }
}
