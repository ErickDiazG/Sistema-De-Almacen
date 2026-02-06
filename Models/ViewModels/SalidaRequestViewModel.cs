using System.ComponentModel.DataAnnotations;

namespace Sistema_Almacen.Models.ViewModels
{
    public class SalidaRequestViewModel
    {
        [Required]
        public string TipoSalida { get; set; } = "CONSUMO"; // CONSUMO o PRESTAMO

        public int? UsuarioSolicitanteId { get; set; } // Si es préstamo o consumo asignado
        public string Referencia { get; set; } = string.Empty; // Proyecto/Razón

        [Required]
        public List<SalidaItemViewModel> Items { get; set; } = new List<SalidaItemViewModel>();
    }

    public class SalidaItemViewModel
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
    }
}
