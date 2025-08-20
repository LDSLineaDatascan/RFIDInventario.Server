using RFIDInventario.Server.Data;
using RFIDInventario.Server.Models;
using static RFIDInventario.Server.Models.FileParams;
using System.Diagnostics;

namespace RFIDInventario.Server.Services
{
    public class InventarioService : ICarga
    {
        private readonly AppDbContext _context;

        public InventarioService(AppDbContext context)
        {
            _context = context;
        }

        public void CargaCsv(string filePath, string fileSeparator, List<FileParam> fileParams)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"El archivo {filePath} no existe.");
                return;
            }

            var lines = File.ReadAllLines(filePath);
            var inventarios = new List<Inventario>();
            var codTiendas = new HashSet<string>();

            for (int i = 1; i < lines.Length; i++)
            {
                Dictionary<string, string> dictionary = [];
                var line = lines[i];
                var parts = line.Split(fileSeparator);

                if (parts.Length != fileParams.Count)
                {
                    Console.WriteLine($"La línea no tiene el número de columnas esperadas: {line}");
                    Debug.WriteLine($"Procesando línea: {line}");
                    continue;
                }

                Console.WriteLine($"Procesando línea: {line}");
                foreach (var item in fileParams)
                {
                    dictionary[item.ColumnName] = parts[item.Position].Trim().Trim('"');
                }

                var inventario = new Inventario
                {
                    IdTienda = dictionary["IdTienda"],
                    IdProducto = dictionary["IdProducto"],
                    Cantidad = int.Parse(dictionary["Cantidad"])
                };

                codTiendas.Add(inventario.IdTienda);
                inventarios.Add(inventario);
            }

            if (inventarios.Count > 0)
            {
                var inventario = _context.InventarioTeorico
                    .Where(i => codTiendas.Contains(i.IdTienda)).ToList();
                var tiendas = _context.Tiendas
                    .Where(t => codTiendas.Contains(t.Codigo)).ToList();

                foreach (var item in tiendas)
                {
                    item.Estado = "";
                }

                _context.RemoveRange(inventario);
                _context.AddRange(inventarios);
                _context.SaveChanges();
                Console.WriteLine("Datos cargados correctamente.");
            }
            else
            {
                Console.WriteLine("No se encontraron datos para cargar");
            }
        }

        public void Cerrar(string idTienda)
        {
            try
            {
                var tienda = _context.Tiendas.Find(idTienda);
                if (tienda != null)
                {
                    tienda.Estado = "Cerrado";
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cerrar la tienda.: {ex.Message}");
            }
        }

        public void Reiniciar(string idTienda)
        {
            try
            {
                var inventarioFisico = _context.InventarioFisico
                    .Where(f => f.IdTienda.Equals(idTienda));
                var tags = _context.TagTienda
                    .Where(t => t.IdTienda.Equals(idTienda));

                _context.RemoveRange(inventarioFisico);
                _context.RemoveRange(tags);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al reiniciar la tienda.: {ex.Message}");
            }
        }
    }
}
