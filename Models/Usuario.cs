using System.ComponentModel.DataAnnotations;

namespace Sistema_Almacen.Models
{
    /// <summary>
    /// Modelo de Usuario para la autenticación del sistema
    /// </summary>
    public class Usuario
    {
        /// <summary>
        /// Identificador único del usuario
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Nombre de usuario para login (único) - Aquí se usa el número de empleado
        /// </summary>
        [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
        [StringLength(50, ErrorMessage = "El nombre de usuario no puede exceder 50 caracteres")]
        public string NombreUsuario { get; set; }

        /// <summary>
        /// Contraseña del usuario (en texto plano por ahora - solo para pruebas)
        /// NOTA: En producción, esto debe estar hasheado (ej. con BCrypt o Identity)
        /// </summary>
        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(100, MinimumLength = 4, ErrorMessage = "La contraseña debe tener entre 4 y 100 caracteres")]
        public string Password { get; set; }

        /// <summary>
        /// Rol del usuario: "Admin" o "Empleado"
        /// Esto se usará para autorización y control de acceso
        /// </summary>
        [Required(ErrorMessage = "El rol es obligatorio")]
        public string Rol { get; set; }
    }
}
