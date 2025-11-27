using System.ComponentModel.DataAnnotations.Schema;

namespace RFIDInventario.Server.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Correo { get; set; } = string.Empty;
        public string? Nombre { get; set; }
        public string Rol { get; set; } = "Admin";
        public bool Activo { get; set; } = true;
        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Column("fecha_actualizado")]
        public DateTime FechaActualizado { get; set; } = DateTime.Now;
    }
}
