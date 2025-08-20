namespace RFIDInventario.Server.Models
{
    public class ReinicioCategoriaRequest
    {
        public string IdTienda { get; set; } = null!;
        public string Categoria { get; set; } = null!;
    }
}
