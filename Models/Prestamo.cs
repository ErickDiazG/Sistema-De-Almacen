using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_Almacen.Models
{
    /// <summary>
    /// Estados posibles de un préstamo
    /// </summary>
    public enum EstatusPrestamo
    {
        Activo,
        Devuelto
    }

    /// <summary>
    /// Modelo para Control de Préstamos de Activos Fijos (Herramientas, Laptops, etc.)
    /// </summary>
    public class Prestamo
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Producto/Activo que se presta
        /// </summary>
        [Required(ErrorMessage = "El producto es obligatorio")]
        public int ProductoId { get; set; }

        /// <summary>
        /// Nombre de la persona que solicita el préstamo
        /// </summary>
        [Required(ErrorMessage = "El solicitante es obligatorio")]
        [StringLength(100)]
        [Display(Name = "Solicitante")]
        public string UsuarioSolicitante { get; set; } = string.Empty;

        /// <summary>
        /// Departamento o área del solicitante
        /// </summary>
        [StringLength(100)]
        [Display(Name = "Departamento")]
        public string? Departamento { get; set; }

        /// <summary>
        /// Fecha y hora en que se prestó el activo
        /// </summary>
        [Required]
        [Display(Name = "Fecha de Salida")]
        public DateTime FechaSalida { get; set; } = DateTime.Now;

        /// <summary>
        /// Fecha límite para devolver el activo (por defecto 7 días)
        /// </summary>
        [Required]
        [Display(Name = "Fecha Esperada de Regreso")]
        public DateTime FechaEsperadaRegreso { get; set; }

        /// <summary>
        /// Fecha real de devolución (null si no ha sido devuelto)
        /// </summary>
        [Display(Name = "Fecha de Devolución")]
        public DateTime? FechaDevolucionReal { get; set; }

        /// <summary>
        /// Estado actual del préstamo
        /// </summary>
        [Required]
        public EstatusPrestamo Estatus { get; set; } = EstatusPrestamo.Activo;

        /// <summary>
        /// Cantidad de unidades prestadas
        /// </summary>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public int Cantidad { get; set; } = 1;

        /// <summary>
        /// Cantidad que ya ha sido devuelta al almacén
        /// </summary>
        public int CantidadDevuelta { get; set; } = 0;

        /// <summary>
        /// Notas o comentarios adicionales
        /// </summary>
        [StringLength(500)]
        public string? Comentarios { get; set; }

        /// <summary>
        /// ID del usuario que registró el préstamo
        /// </summary>
        public int? UsuarioRegistroId { get; set; }

        // =====================================================
        // PROPIEDADES CALCULADAS
        // =====================================================

        /// <summary>
        /// Indica si el préstamo está atrasado (pasó la fecha esperada y sigue activo)
        /// </summary>
        [NotMapped]
        public bool EstaAtrasado => Estatus == EstatusPrestamo.Activo && DateTime.Now > FechaEsperadaRegreso;

        /// <summary>
        /// Días de atraso (negativo si aún tiene tiempo)
        /// </summary>
        /// <summary>
        /// Días de atraso (negativo si aún tiene tiempo)
        /// </summary>
        [NotMapped]
        public int DiasAtraso => Estatus == EstatusPrestamo.Activo 
            ? (int)(DateTime.Now - FechaEsperadaRegreso).TotalDays 
            : 0;

        /// <summary>
        /// Días restantes para devolver (positivo si aún tiene tiempo)
        /// </summary>
        [NotMapped]
        public int DiasRestantes => Estatus == EstatusPrestamo.Activo 
            ? (int)(FechaEsperadaRegreso - DateTime.Now).TotalDays 
            : 0;

        // =====================================================
        // NAVEGACIÓN
        // =====================================================

        [ForeignKey("ProductoId")]
        public virtual Producto Producto { get; set; } = null!;

        [ForeignKey("UsuarioRegistroId")]
        public virtual Usuario? UsuarioRegistro { get; set; }
    }
}
