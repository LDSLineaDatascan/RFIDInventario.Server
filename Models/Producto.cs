using System.ComponentModel.DataAnnotations;

namespace RFIDInventario.Server.Models
{
    public class Producto
    {
        [Key]
        public required string Codigo { get; set; }
        public required string Nombre { get; set; }
        public required string Categoria { get; set; }
    }
}
