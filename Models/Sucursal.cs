using System.ComponentModel.DataAnnotations;

namespace Sistema_Almacen.Models
{
    public class Sucursal
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la sucursal es obligatorio")]
        [StringLength(100)]
        public string Nombre { get; set; }

        // Navegación
        public virtual ICollection<LoteInventario> Lotes { get; set; } = new List<LoteInventario>();
    }
}
