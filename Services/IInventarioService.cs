using System.Threading.Tasks;

namespace Sistema_Almacen.Services
{
    public interface IInventarioService
    {
        Task DescontarStockFIFO(int productoId, int cantidad, int? sucursalId, int? ubicacionId = null);
        Task<int> ObtenerStockDisponible(int productoId, int? sucursalId);
    }
}
