using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_Almacen.Models
{
    public enum TipoMovimiento
    {
        Entrada,
        Salida,
        Ajuste
    }

    public class MovimientoAlmacen
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required]
        public int UsuarioId { get; set; }

        [Required]
        public TipoMovimiento Tipo { get; set; }

        [Required]
        public int Cantidad { get; set; }

        [StringLength(255)]
        public string? Referencia { get; set; }

        public int? ProductoId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CostoUnitario { get; set; } = 0;

        // Navegación
        [ForeignKey("UsuarioId")]
        public virtual Usuario Usuario { get; set; } = null!;

        [ForeignKey("ProductoId")]
        public virtual Producto? Producto { get; set; }
    }
}
