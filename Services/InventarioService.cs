using Microsoft.EntityFrameworkCore;
using Sistema_Almacen.Data;
using Sistema_Almacen.Models;

namespace Sistema_Almacen.Services
{
    public class InventarioService : IInventarioService
    {
        private readonly ApplicationDbContext _context;

        public InventarioService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task DescontarStockFIFO(int productoId, int cantidad, int? sucursalId, int? ubicacionId = null)
        {
            // 1. Validar parámetros
            if (cantidad <= 0) throw new ArgumentException("La cantidad debe ser mayor a 0", nameof(cantidad));

            // 2. Construir query base
            var query = _context.LotesInventario
                .Where(l => l.ProductoId == productoId && l.StockActual > 0);

            if (sucursalId.HasValue)
            {
                query = query.Where(l => l.SucursalId == sucursalId.Value);
            }

            if (ubicacionId.HasValue)
            {
                query = query.Where(l => l.UbicacionId == ubicacionId.Value);
            }

            // 3. Ordenar por FIFO
            var lotes = await query
                .OrderBy(l => l.FechaEntrada)
                .ToListAsync();

            // 4. Verificar stock total suficiente
            int stockTotal = lotes.Sum(l => l.StockActual);
            if (stockTotal < cantidad)
            {
                throw new InvalidOperationException($"Stock insuficiente para el producto ID {productoId}. Requerido: {cantidad}, Disponible: {stockTotal}");
            }

            // 5. Descontar stock
            int cantidadRestante = cantidad;
            foreach (var lote in lotes)
            {
                if (cantidadRestante <= 0) break;

                int cantidadASacar = Math.Min(lote.StockActual, cantidadRestante);
                
                lote.StockActual -= cantidadASacar;
                cantidadRestante -= cantidadASacar;

                _context.LotesInventario.Update(lote);
            }
        }

        public async Task<int> ObtenerStockDisponible(int productoId, int? sucursalId)
        {
             var query = _context.LotesInventario
                .Where(l => l.ProductoId == productoId && l.StockActual > 0);

            if (sucursalId.HasValue)
            {
                query = query.Where(l => l.SucursalId == sucursalId.Value);
            }

            return await query.SumAsync(l => l.StockActual);
        }
    }
}
