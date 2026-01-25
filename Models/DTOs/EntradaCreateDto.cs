using System.ComponentModel.DataAnnotations;

namespace Sistema_Almacen.Models.DTOs
{
    /// <summary>
    /// DTO para la creación de entradas de mercancía al almacén.
    /// Encapsula todos los parámetros necesarios para registrar una entrada.
    /// </summary>
    public class EntradaCreateDto
    {
        [Required(ErrorMessage = "El producto es requerido")]
        [Display(Name = "Producto")]
        public int ProductoId { get; set; }

        [Required(ErrorMessage = "El proveedor es requerido")]
        [Display(Name = "Proveedor")]
        public int ProveedorId { get; set; }

        [Required(ErrorMessage = "La ubicación es requerida")]
        [Display(Name = "Ubicación")]
        public int UbicacionId { get; set; }

        [Required(ErrorMessage = "La sucursal es requerida")]
        [Display(Name = "Sucursal")]
        public int SucursalId { get; set; }

        [Required(ErrorMessage = "La cantidad es requerida")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero")]
        [Display(Name = "Cantidad")]
        public int Cantidad { get; set; }

        [Required(ErrorMessage = "El costo unitario es requerido")]
        [Range(0, double.MaxValue, ErrorMessage = "El costo unitario no puede ser negativo")]
        [Display(Name = "Costo Unitario")]
        public decimal CostoUnitario { get; set; }
    }
}
