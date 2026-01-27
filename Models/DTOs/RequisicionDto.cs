using System.ComponentModel.DataAnnotations;

namespace Sistema_Almacen.Models.DTOs
{
    /// <summary>
    /// DTO para un item individual en la requisición
    /// </summary>
    public class ItemRequisicionDto
    {
        [Required]
        public int ProductoId { get; set; }
        
        public string? ProductoNombre { get; set; }
        
        public string? SKU { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public int Cantidad { get; set; }

        [Required]
        public int SucursalId { get; set; }
        
        public string? SucursalNombre { get; set; }
    }

    /// <summary>
    /// DTO para procesar la requisición completa (múltiples items)
    /// </summary>
    public class RequisicionDto
    {
        [Required]
        public List<ItemRequisicionDto> Items { get; set; } = new List<ItemRequisicionDto>();

        [StringLength(255)]
        public string? Referencia { get; set; }
    }
}
