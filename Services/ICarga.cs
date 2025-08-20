using RFIDInventario.Server.Models;
using static RFIDInventario.Server.Models.FileParams;

namespace RFIDInventario.Server.Services
{
    public interface ICarga
    {
        void CargaCsv(string filePath, string fileSeparator, List<FileParam> fileParams);
        //void Cerrar(string idTienda);
        //void Reiniciar(string idTienda);
    }
}