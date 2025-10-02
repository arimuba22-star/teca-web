using System;

namespace SWebEnergia.Models
{
    public partial class Usuario
    {
        public int IdUsuario { get; set; }    // Identificador único del usuario
        public int IdRol { get; set; }         // Rol asociado al usuario (clave foránea)
        public string Nombre { get; set; }     // Nombre del usuario
        public string Email { get; set; }      // Email del usuario
        public string? Salt { get; set; } // antes era string (no nullable)
        public string? PasswordHash { get; set; } // si lo manejas internamente

        public DateTime? FechaCreacion { get; set; } // Fecha de creación del usuario

        public bool? Activo { get; set; }      // Indica si el usuario está activo

        public DateTime? FechaNacimiento { get; set; }


        public virtual Role? IdRolNavigation { get; set; }  // Relación con la entidad "Role" (tipo de rol)
        public virtual Tecnico? Tecnico { get; set; }      // Relación opcional con "Tecnico" (si aplica)
    }
}
