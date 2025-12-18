using RFIDInventario.Server.Data;
using RFIDInventario.Server.Models;
using static RFIDInventario.Server.Models.FileParams;

namespace RFIDInventario.Server.Services
{
    public class TiendaService(AppDbContext context) : ICarga
    {
        private readonly AppDbContext _context = context;

        // Método para leer el archivo y cargar los datos en la base de datos
        public void CargaCsv(string filePath, string fileSeparator, List<FileParam> fileParams)
        {
            // Verificar que el archivo existe
            if (!File.Exists(filePath))
            {
                Console.WriteLine("El archivo no existe.");
                return;
            }

            // Abrir el archivo y leer línea por línea
            var lines = File.ReadLines(filePath);
            var tiendas = new List<Tienda>();

            for (int i = 1; i < lines.Count(); i++)
            {
                Dictionary<string, string> dictionary = [];
                var line = lines.ElementAt(i);
                // Separar la línea por comas
                var parts = line.Split(fileSeparator);

                if (parts.Length != 4)
                {
                    Console.WriteLine($"La línea no tiene el formato esperado: {line}");
                    continue;
                }

                Console.WriteLine(line);
                foreach (var item in fileParams.AsParallel())
                {
                    dictionary[item.ColumnName] = parts[item.Position];
                }

                var tienda = new Tienda
                {
                    Codigo = (string)dictionary["Codigo"],
                    Nombre = (string)dictionary["Nombre"],
                    Estado = (string)dictionary["Estado"]
                    //Estado=""->Estado segun video
                };

                tiendas.Add(tienda);
            }

            // guardo los datos en db
            if (tiendas.Count > 0)
            {
                _context.RemoveRange(_context.InventarioTeorico);
                _context.AddRange(tiendas);
                _context.SaveChanges();
                Console.WriteLine("Datos cargados correctamente.");
            }
            else
            {
                Console.WriteLine("No se encontraron datos para cargar.");
            }
        }
    }
}
