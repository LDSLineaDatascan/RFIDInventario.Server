using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RFIDInventario.Server.Models
{
    [Table("USUARIOS_TIENDAS")]
    public class UsuariosTiendas
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("usuario_id")]
        public int UsuarioId { get; set; }

        [Column("tienda_codigo")]
        public string TiendaCodigo { get; set; } = string.Empty;

        [Column("rol_asignado")]
        public string RolAsignado { get; set; } = string.Empty;

        [Column("fecha_asignacion")]
        public DateTime FechaAsignacion { get; set; }

        [Column("asignado_por_id")]
        public int? AsignadoPorId { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }

        [ForeignKey("AsignadoPorId")]
        public Usuario? AsignadoPor { get; set; }
    }
}
