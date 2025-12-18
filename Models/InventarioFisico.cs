using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RFIDInventario.Server.Models
{


    [Table("INVENTARIO_FISICO")]
    public class InventarioFisico
    {
        [Key, Column("ID_PRODUCTO", Order = 0)]
        public required string IdProducto { get; set; }
        
        
        [Key, Column("ID_TIENDA", Order = 1)]
        public required string IdTienda { get; set; }
        
        
        [Column("CANTIDAD_LECTURA", Order =2)]
        public required int CantidadLeida { get; set; }
    }
}
