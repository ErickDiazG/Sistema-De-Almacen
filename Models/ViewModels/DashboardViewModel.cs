namespace Sistema_Almacen.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalProductos { get; set; }
        public int ProductosStockBajo { get; set; }
        public decimal ValorInventario { get; set; }
        public int VentasHoy { get; set; }
        public decimal IngresosHoy { get; set; }
        
        public int TotalPrestamosVencidos { get; set; } // Nuevo KPI
        
        // Listas para mostrar información rápida
        // public List<Producto>? ProductosRecientes { get; set; } // Eliminado
        public List<MovimientoAlmacen>? UltimosMovimientos { get; set; } // Nueva Tabla

        // Datos para la gráfica
        public string[] ChartLabels { get; set; } = Array.Empty<string>();
        public int[] ChartDataEntradas { get; set; } = Array.Empty<int>();
        public int[] ChartDataSalidas { get; set; } = Array.Empty<int>();
    }
}
