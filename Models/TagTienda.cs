using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RFIDInventario.Server.Models
{
    [Table("TAGS_TIENDAS")]

    public class TagTienda
    {
        [Key, Column("ID_TIENDA", Order =0)]
        public required string IdTienda { get; set; }

        [Key, Column("TAG", Order = 1)]

        public required string Tag { get; set; }
        public required string Ean { get; set; }
        public required DateTime Fecha { get; set; }

        public TagTienda() { }
        public TagTienda(string idTienda, string tag, string ean, DateTime fecha)
        {
            IdTienda = idTienda;
            Tag = tag;
            Ean = ean;
            Fecha = fecha;
        }
    }


}
