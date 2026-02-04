
namespace Sistema_Almacen.Models.ViewModels
{
    public class ImportPreviewViewModel
    {
        public int RowIndex { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public decimal Costo { get; set; }
        public int Cantidad { get; set; }
        public string UbicacionCodigo { get; set; } = string.Empty;
        
        // Validation properties
        public bool IsValid { get; set; } = true;
        public string ErrorMessage { get; set; } = string.Empty;
        public bool ExistsInDb { get; set; } = false;
    }
}
