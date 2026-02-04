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

        // Jerarquía de Almacén (Intelligent Layout)
        [StringLength(20)]
        public string Zona { get; set; } = "A"; // Ej: A, B, Recepción

        [StringLength(20)]
        public string Pasillo { get; set; } = "01"; 

        [StringLength(20)]
        public string Rack { get; set; } = "01";

        [StringLength(20)]
        public string Nivel { get; set; } = "1";

        /// <summary>
        /// Devuelve el Picking Path formateado: "ZONA-A / AISLE-01 / RACK-03 / LVL-2"
        /// </summary>
        public string PickingPath => $"{Zona} / {Pasillo} / {Rack} / {Nivel}";

        // Navegación
        public virtual ICollection<LoteInventario> Lotes { get; set; } = new List<LoteInventario>();
    }
}
