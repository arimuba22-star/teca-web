using System.ComponentModel.DataAnnotations;

namespace SWebEnergia.Models
{
    public partial class Cliente
    {
        public int IdCliente { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(120, ErrorMessage = "El nombre no puede tener más de 120 caracteres.")]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
        public string Email { get; set; } = null!;

        [StringLength(9, ErrorMessage = "El teléfono no puede tener más de 9 caracteres.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "El teléfono debe contener solo números.")]
        public string? Telefono { get; set; }

        [StringLength(200, ErrorMessage = "La dirección no puede tener más de 200 caracteres.")]
        public string? Direccion { get; set; }

        // Nuevos campos
        [Required(ErrorMessage = "El tipo de documento es obligatorio.")]
        [StringLength(50, ErrorMessage = "El tipo de documento no puede tener más de 50 caracteres.")]
        public string? TDocumento { get; set; }  // Tipo de documento (RUC, DNI o Carnet de Extranjería)

        [Required(ErrorMessage = "El número de documento es obligatorio.")]
        public string? NDocumento { get; set; } // Número de documento

        public DateTime? FechaRegistro { get; set; }

        public DateTime? FechaUltimaActualizacion { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Nacimiento")]
        public DateTime? FechaNacimiento { get; set; }


        // Relaciones con otras entidades
        public virtual ICollection<Comprobante> Comprobantes { get; set; } = new List<Comprobante>();
        public virtual ICollection<Venta> Venta { get; set; } = new List<Venta>();
        public ICollection<SistemasRenovable> SistemasRenovables { get; set; } = new List<SistemasRenovable>();
        public ICollection<Mantenimiento> Mantenimientos { get; set; } = new List<Mantenimiento>();
    }

}
