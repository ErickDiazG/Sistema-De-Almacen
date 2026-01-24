namespace Sistema_Almacen.Models.ViewModels
{
    public class InventarioViewModel
    {
        public string Producto { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string Sucursal { get; set; } = string.Empty;
        public int SucursalId { get; set; }
        public int StockTotal { get; set; }
        public string Estado => GetEstado();

        private string GetEstado()
        {
            if (StockTotal < 10) return "Bajo";
            if (StockTotal < 50) return "Medio";
            return "Suficiente";
        }
    }
}
