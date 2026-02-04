using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_Almacen.Models
{
    public class Producto
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del producto es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El SKU es obligatorio")]
        [StringLength(50, ErrorMessage = "El SKU no puede exceder los 50 caracteres")]
        public string SKU { get; set; } = string.Empty;

        [Required(ErrorMessage = "El stock mínimo es obligatorio")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock mínimo debe ser mayor o igual a 0")]
        public int StockMinimo { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "El precio de venta debe ser mayor o igual a 0")]
        public decimal PrecioVenta { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "El costo promedio debe ser mayor o igual a 0")]
        public decimal CostoPromedio { get; set; }

        [Display(Name = "Imagen URL")]
        public string? ImagenURL { get; set; }

        /// <summary>
        /// Indica si el producto es un activo fijo (herramienta, laptop, etc.) que puede ser prestado
        /// </summary>
        [Display(Name = "Es Activo Fijo")]
        /// <summary>
        /// Indica si el producto es un activo fijo (herramienta, laptop, etc.) que puede ser prestado
        /// </summary>
        [Display(Name = "Es Activo Fijo")]
        public bool EsActivoFijo { get; set; } = false;

        /// <summary>
        /// Propiedad solicitada "Es Prestable" (Alias para UI o lógica específica)
        /// </summary>
        [Display(Name = "Es Prestable")]
        public bool EsPrestable { get; set; } = false;

        [Display(Name = "Ubicación por Defecto")]
        public int? UbicacionDefectoId { get; set; }

        [ForeignKey("UbicacionDefectoId")]
        public virtual Ubicacion? UbicacionDefecto { get; set; }

        [Required(ErrorMessage = "La categoría es obligatoria")]
        public int CategoriaId { get; set; }

        // Navegación
        [ForeignKey("CategoriaId")]
        public virtual Categoria Categoria { get; set; } = null!;

        public virtual ICollection<LoteInventario> Lotes { get; set; } = new List<LoteInventario>();
        public virtual ICollection<Producto> ProductosConDefecto { get; set; } = new List<Producto>();

        public virtual ICollection<Prestamo> Prestamos { get; set; } = new List<Prestamo>();
    }
}
