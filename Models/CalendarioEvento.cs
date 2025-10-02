using System.ComponentModel.DataAnnotations;

namespace SWebEnergia.Models
{
    public class CalendarioEvento
    {
        // Identificador opcional si quieres enlazar con un mantenimiento específico
        public int? IdMantenimiento { get; set; }

        [Required(ErrorMessage = "El título del evento es obligatorio.")]
        [StringLength(150, ErrorMessage = "El título no puede tener más de 150 caracteres.")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
        [DataType(DataType.Date)]
        public string Start { get; set; } = null!; // ISO 8601: yyyy-MM-dd

        [StringLength(20)]
        public string? Color { get; set; } // HEX o nombre (opcional, para diferenciar tipos)

        [StringLength(500, ErrorMessage = "La descripción no puede tener más de 500 caracteres.")]
        public string? Descripcion { get; set; }

        // Campos opcionales que podrías necesitar para mostrar tooltips o más datos
        [StringLength(120)]
        public string? NombreCliente { get; set; }

        [EmailAddress(ErrorMessage = "Correo electrónico inválido.")]
        public string? EmailCliente { get; set; }
    }
}