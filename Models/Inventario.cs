using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RFIDInventario.Server.Models
{
  

    [Table("INVENTARIO_TEORICO")]
    public class Inventario
    {
        [Key, Column("ID_Producto", Order = 0)]
        public required string IdProducto { get; set; }


        [Key, Column("ID_TIENDA", Order = 1)]
        public required string IdTienda { get; set; }


        public required int Cantidad { get; set; }
    }


}
