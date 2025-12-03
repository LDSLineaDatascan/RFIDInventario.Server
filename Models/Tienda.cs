using System.ComponentModel.DataAnnotations;

namespace RFIDInventario.Server.Models
{
    public class Tienda
    {
        [Key]
        public required string Codigo { get; set; }

        [Required]
        public required string Nombre { get; set; }
        [Required]
        public required string Estado_Conteo { get; set; }
    }
}
