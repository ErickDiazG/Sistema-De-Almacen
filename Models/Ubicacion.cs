using System.ComponentModel.DataAnnotations;

namespace Sistema_Almacen.Models
{
    public class Ubicacion
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la ubicación es obligatorio")]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El código de ubicación es obligatorio")]
        [StringLength(20)]
        public string Codigo { get; set; } = string.Empty;

        // Navegación
        public virtual ICollection<LoteInventario> Lotes { get; set; } = new List<LoteInventario>();
    }
}
