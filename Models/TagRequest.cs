namespace RFIDInventario.Server.Models
{
    public class TagRequest
    {
        public required string Tag { get; set; }
        public required string Ean { get; set; }
    }
}
