using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_Almacen.Models
{
    public class Producto
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del producto es obligatorio")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El SKU es obligatorio")]
        [StringLength(50)]
        public string SKU { get; set; } = string.Empty;

        [Required(ErrorMessage = "El stock mínimo es obligatorio")]
        public int StockMinimo { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioVenta { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CostoPromedio { get; set; }

        public string? ImagenURL { get; set; }

        [Required]
        public int CategoriaId { get; set; }

        // Navegación
        [ForeignKey("CategoriaId")]
        public virtual Categoria Categoria { get; set; } = null!;

        public virtual ICollection<LoteInventario> Lotes { get; set; } = new List<LoteInventario>();
    }
}
