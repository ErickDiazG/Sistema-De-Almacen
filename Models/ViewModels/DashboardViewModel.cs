namespace Sistema_Almacen.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalProductos { get; set; }
        public int ProductosStockBajo { get; set; }
        public decimal ValorInventario { get; set; }
        public int VentasHoy { get; set; }
        public decimal IngresosHoy { get; set; }
        
        // Listas para mostrar información rápida
        public List<Producto>? ProductosRecientes { get; set; }
    }
}
