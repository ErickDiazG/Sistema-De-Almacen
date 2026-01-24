using Microsoft.EntityFrameworkCore;
using Sistema_Almacen.Data;
using Sistema_Almacen.Models;

namespace Sistema_Almacen.Services
{
    public interface IVentaService
    {
        Task ProcesarVentaFIFO(int sucursalId, int productoId, int cantidad);
    }

    public class VentaService : IVentaService
    {
        private readonly ApplicationDbContext _context;

        public VentaService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task ProcesarVentaFIFO(int sucursalId, int productoId, int cantidad)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Buscar lotes disponibles ordenados por FechaEntrada (FIFO)
                    var lotes = await _context.LotesInventario
                        .Where(l => l.ProductoId == productoId && l.SucursalId == sucursalId && l.StockActual > 0)
                        .OrderBy(l => l.FechaEntrada)
                        .ToListAsync();

                    int stockTotal = lotes.Sum(l => l.StockActual);

                    if (stockTotal < cantidad)
                    {
                        throw new Exception($"Stock insuficiente. Requerido: {cantidad}, Disponible: {stockTotal}");
                    }

                    int cantidadRestante = cantidad;
                    decimal totalVenta = 0;

                    // 2. Buscar el producto una sola vez
                    var producto = await _context.Productos.FindAsync(productoId);
                    if (producto == null) throw new Exception("Producto no encontrado");

                    // 3. Crear cabecera de la venta
                    var venta = new Venta
                    {
                        Fecha = DateTime.Now,
                        Total = 0 // Se actualizará al final
                    };
                    _context.Ventas.Add(venta);
                    await _context.SaveChangesAsync();

                    // 4. Iterar sobre los lotes para restar el stock
                    foreach (var lote in lotes)
                    {
                        if (cantidadRestante <= 0) break;

                        int cantidadASacar = Math.Min(lote.StockActual, cantidadRestante);
                        
                        lote.StockActual -= cantidadASacar;
                        cantidadRestante -= cantidadASacar;
                        
                        var detalle = new DetalleVenta
                        {
                            VentaId = venta.Id,
                            ProductoId = productoId,
                            Cantidad = cantidadASacar,
                            PrecioUnitario = producto.PrecioVenta
                        };
                        _context.DetallesVenta.Add(detalle);
                        
                        totalVenta += detalle.Cantidad * detalle.PrecioUnitario;
                    }

                    // 4. Actualizar el total de la venta
                    venta.Total = totalVenta;
                    await _context.SaveChangesAsync();

                    // 5. Confirmar transacción
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }
    }
}
