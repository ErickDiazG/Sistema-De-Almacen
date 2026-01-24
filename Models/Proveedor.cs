using System.ComponentModel.DataAnnotations;

namespace Sistema_Almacen.Models
{
    public class Proveedor
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del proveedor es obligatorio")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Contacto { get; set; }

        [StringLength(20)]
        public string? Telefono { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        // Navegación
        public virtual ICollection<LoteInventario> Lotes { get; set; } = new List<LoteInventario>();
    }
}
