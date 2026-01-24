using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_Almacen.Models
{
    public class LoteInventario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductoId { get; set; }

        public int? ProveedorId { get; set; }

        [Required]
        public int UbicacionId { get; set; }

        [Required]
        public int SucursalId { get; set; }

        [Required]
        public int CantidadInicial { get; set; }

        [Required]
        public int StockActual { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CostoUnitario { get; set; }

        [Required]
        public DateTime FechaEntrada { get; set; }

        // Navegación
        [ForeignKey("ProductoId")]
        public virtual Producto Producto { get; set; } = null!;

        [ForeignKey("ProveedorId")]
        public virtual Proveedor? Proveedor { get; set; }

        [ForeignKey("UbicacionId")]
        public virtual Ubicacion Ubicacion { get; set; } = null!;

        [ForeignKey("SucursalId")]
        public virtual Sucursal Sucursal { get; set; } = null!;
    }
}
