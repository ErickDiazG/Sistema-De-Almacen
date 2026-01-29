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
        private readonly IInventarioService _inventarioService;

        public VentaService(ApplicationDbContext context, IInventarioService inventarioService)
        {
            _context = context;
            _inventarioService = inventarioService;
        }

        public async Task ProcesarVentaFIFO(int sucursalId, int productoId, int cantidad)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Usar el servicio de inventario para descontar stock FIFO
                    await _inventarioService.DescontarStockFIFO(productoId, cantidad, sucursalId);

                    decimal totalVenta = 0;

                    // 2. Buscar el producto para obtener precio
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

                    // 4. Crear detalle de venta (Simplificado ya que el stock se descontó globalmente)
                    // Nota: Si se requiere rastrear qué lotes específicos se vendieron en el detalle, 
                    // el servicio de inventario debería devolver esa información. 
                    // Por ahora asumimos que solo necesitamos registrar que se vendió tal producto a tal precio.
                    
                    var detalle = new DetalleVenta
                    {
                        VentaId = venta.Id,
                        ProductoId = productoId,
                        Cantidad = cantidad,
                        PrecioUnitario = producto.PrecioVenta
                    };
                    _context.DetallesVenta.Add(detalle);
                    
                    totalVenta = detalle.Cantidad * detalle.PrecioUnitario;

                    // 5. Actualizar el total de la venta
                    venta.Total = totalVenta;
                    await _context.SaveChangesAsync();

                    // 6. Confirmar transacción
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
