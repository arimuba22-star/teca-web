using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace SWebEnergia.Models.Metadata
{
    public class ClientesMetadata
    {
        [Required, StringLength(120)]
        public string Nombre { get; set; } = null!;

        [Required, EmailAddress, StringLength(150)]
        public string Email { get; set; } = null!;

        [Required, StringLength(30)]
        public string Telefono { get; set; } = null!;

        [StringLength(200)]
        public string? Direccion { get; set; }

        // Eliminar la propiedad Rfc que ya no está en el modelo ni en la base de datos
        // [Display(Name = "RFC"), StringLength(20)]
        // public string? Rfc { get; set; }
    }

    [ModelMetadataType(typeof(ClientesMetadata))]
    public partial class Clientes { }
}
