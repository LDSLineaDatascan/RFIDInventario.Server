using Microsoft.AspNetCore.SignalR;
using RFIDInventario.Server.Data;
using RFIDInventario.Server.Hubs;
using RFIDInventario.Server.Models;
using Microsoft.EntityFrameworkCore;


namespace RFIDInventario.Server.Services
{
    public class DirectoryWatcherService
    {
        private readonly FileSystemWatcher _fileSystemWatcher;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly FileParams.FileParamsSettings _fileParamsSettings;

        //actualizacion de inventario teórico automático
        private readonly IHubContext<NotificationHub> _hubContext;


        public DirectoryWatcherService(IServiceScopeFactory serviceScopeFactory,
            FileParams.FileParamsSettings fileParamsSettings,
            IHubContext<NotificationHub> hubContext)


        {
            _serviceScopeFactory = serviceScopeFactory;
            _fileParamsSettings = fileParamsSettings;
            _hubContext = hubContext;

            //Verifico si el direcotrio existe
            if (!Directory.Exists(fileParamsSettings.DirectoryPath))
            {
                throw new DirectoryNotFoundException($"El directorio {fileParamsSettings.DirectoryPath} no existe.");
            }

            _fileSystemWatcher = new FileSystemWatcher(fileParamsSettings.DirectoryPath)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                Filter = "*.csv"
            };

            //suscribo a los eventos
            _fileSystemWatcher.Created += OnFileCreated;
            _fileSystemWatcher.Changed += OnFileChanged;
            _fileSystemWatcher.Deleted += OnFileDeleted;
        }
        //metodo para inciar watch del direcotorio
        public void StartListening()
        {
            _fileSystemWatcher.EnableRaisingEvents = true;
            Console.WriteLine($"Escuchando el directorio: {_fileParamsSettings.DirectoryPath}");
        }

        //Detener vigilancia del directorio
        public void StopListening()
        {
            _fileSystemWatcher.EnableRaisingEvents = false;
            Console.WriteLine($"Finalizar listening del directorio: {_fileParamsSettings.DirectoryPath}");
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"Nuevo archivo creado: {e.FullPath}");
            var modo = e.Name?.Split(".")[0].Trim().Split("_")[0];
            using var scope = _serviceScopeFactory.CreateScope();
            var fileSettings = _fileParamsSettings.Files.Find(fileParam =>
            {
                if (fileParam.FileName.Equals(modo))
                {
                    switch (modo)
                    {
                        case "INVENTARIOS":
                            fileParam.Service = scope.ServiceProvider.GetRequiredService<InventarioService>();
                            break;
                        case "PRODUCTOS":
                            fileParam.Service = scope.ServiceProvider.GetRequiredService<ProductoService>();
                            break;
                        case "TIENDAS":
                            fileParam.Service = scope.ServiceProvider.GetRequiredService<TiendaService>();
                            break;
                    }

                    return true;
                }

                return false;
            });

            if (fileSettings != null)
            {
                fileSettings.Service?.CargaCsv(e.FullPath, fileSettings.FileSeparator, fileSettings.FileParams);
                File.Delete(e.FullPath);
            }
        }

        // Event handler cuando un archivo es modificado
        private async void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"Archivo modificado: {e.FullPath}");
            //ProcesarArchivo(e);
            await Task.Delay(500); // Pequeña espera para que el archivo termine de guardarse
            await Task.Run(() => ProcesarArchivo(e)); // Ejecuta y espera la actualización

        }

        private async Task ProcesarArchivo(FileSystemEventArgs e)
        {
            var modo = Path.GetFileNameWithoutExtension(e.Name)?.Trim().ToLower();

            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var fileSettings = _fileParamsSettings.Files.Find(fileParam =>
                fileParam.FileName.Equals(modo, StringComparison.OrdinalIgnoreCase)
            );

            if (fileSettings != null)
            {
                // Asignar servicio según el archivo
                switch (modo)
                {
                    case "inventario_teorico":
                        fileSettings.Service = scope.ServiceProvider.GetRequiredService<InventarioService>();
                        break;
                    case "productos":
                        fileSettings.Service = scope.ServiceProvider.GetRequiredService<ProductoService>();
                        break;
                    case "tiendas":
                        fileSettings.Service = scope.ServiceProvider.GetRequiredService<TiendaService>();
                        break;
                }

                //******************************************************************************************
                //***validacion de estado, todas cerradas
                if (string.Equals(modo, "inventario_teorico", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        // Leo todas las líneas y salto las vacías
                        var lines = File.ReadAllLines(e.FullPath)
                                        .Where(l => !string.IsNullOrWhiteSpace(l))
                                        .ToArray();

                        if (lines.Length == 0)
                        {
                            Console.WriteLine("Archivo vacío: " + e.FullPath);
                            // muevo a rechazados
                            MoveToRejected(e.FullPath, "Archivo vacío");
                            await _hubContext.Clients.All.SendAsync("InventarioRechazado", new { archivo = e.Name, razon = "Archivo vacío" });
                            return;
                        }

                        // Determinar separador y columna de IdTienda
                        var separator = fileSettings.FileSeparator ?? ",";
                        var header = lines[0].Split(separator);
                        int idTiendaIndex = -1;

                        // Intentamos encontrar columna con nombre parecido a IdTienda en FileParams
                        var idParam = fileSettings.FileParams?.FirstOrDefault(p => p.ColumnName.Equals("IdTienda", StringComparison.OrdinalIgnoreCase)
                                                                                  || p.ColumnName.Equals("idtienda", StringComparison.OrdinalIgnoreCase)
                                                                                  || p.ColumnName.Equals("tienda", StringComparison.OrdinalIgnoreCase));
                        if (idParam != null)
                        {
                            idTiendaIndex = idParam.Position;
                        }
                        else
                        {
                            // fallback: buscar en header por nombres comunes
                            for (int i = 0; i < header.Length; i++)
                            {
                                var h = header[i].Trim().ToLower();
                                if (h == "idtienda" || h == "tienda" || h == "id_tienda")
                                {
                                    idTiendaIndex = i;
                                    break;
                                }
                            }
                        }

                        if (idTiendaIndex < 0)
                        {
                            Console.WriteLine("No se pudo determinar la columna IdTienda en el CSV: " + e.FullPath);
                            MoveToRejected(e.FullPath, "No se encontró columna IdTienda");
                            await _hubContext.Clients.All.SendAsync("InventarioRechazado", new { archivo = e.Name, razon = "No se encontró columna IdTienda" });
                            return;
                        }

                        // Extraer lista de tiendas únicas desde las líneas (si header está presente, saltarlo)
                        var dataLines = lines.Skip(1); // asumimos que hay header
                        var tiendasEnArchivo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        foreach (var line in dataLines)
                        {
                            var cols = line.Split(separator);
                            if (cols.Length > idTiendaIndex)
                            {
                                var codigo = cols[idTiendaIndex].Trim();
                                if (!string.IsNullOrEmpty(codigo))
                                    tiendasEnArchivo.Add(codigo);
                            }
                        }

                        if (!tiendasEnArchivo.Any())
                        {
                            Console.WriteLine("No se encontraron tiendas en el archivo: " + e.FullPath);
                            MoveToRejected(e.FullPath, "No se encontraron tiendas en el archivo");
                            await _hubContext.Clients.All.SendAsync("InventarioRechazado", new { archivo = e.Name, razon = "No se encontraron tiendas en el archivo" });
                            return;
                        }

                        // Consultar la base de datos si alguna de esas tiendas está ABIERTO
                        var tiendasList = tiendasEnArchivo.ToList();

                        var tiendasAbiertas = await dbContext.Tiendas
                            .Where(t => tiendasList.Contains(t.Codigo) &&
                                        t.Estado_Conteo != null &&
                                        t.Estado_Conteo.ToUpper() == "ABIERTO")
                            .Select(t => new { t.Codigo, t.Nombre })
                            .ToListAsync();

                        if (tiendasAbiertas.Any())
                        {
                            var codigos = string.Join(", ", tiendasAbiertas.Select(t => t.Codigo));
                            Console.WriteLine($"Procesamiento CANCELADO. Tiendas abiertas encontradas en archivo: {codigos}");
                            MoveToRejected(e.FullPath, $"Tiendas abiertas: {codigos}");
                            await _hubContext.Clients.All.SendAsync("InventarioRechazado",
                                new { archivo = e.Name, razon = $"Tiendas abiertas: {codigos}" });

                            return; // no procesar
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error validando tiendas en CSV: {ex}");
                        MoveToRejected(e.FullPath, $"Error validación: {ex.Message}");
                        await _hubContext.Clients.All.SendAsync("InventarioRechazado", new { archivo = e.Name, razon = "Error validación interna" });
                        return;
                    }
                }

                //******************************************************************
                // ⚠️ IMPORTANTE: Solo llamar CargaCsv si NO es inventario_teorico
                // El procedimiento almacenado CARGAR_INVENTARIO_TEORICO ya hace el BULK INSERT
                if (!string.Equals(modo, "inventario_teorico", StringComparison.OrdinalIgnoreCase))
                {
                    // 1️⃣ Procesar CSV para otros archivos (productos, tiendas, etc.)
                    fileSettings.Service?.CargaCsv(e.FullPath, fileSettings.FileSeparator, fileSettings.FileParams);
                }
                else
                {
                    Console.WriteLine("Saltando CargaCsv para inventario_teorico - el procedimiento almacenado maneja la carga");
                }

                // 2️⃣ Ejecutar el procedimiento almacenado (solo para inventario_teorico)
                if (string.Equals(modo, "inventario_teorico", StringComparison.OrdinalIgnoreCase))
                {
                    await dbContext.Database.ExecuteSqlRawAsync("EXEC CARGAR_INVENTARIO_TEORICO");
                    Console.WriteLine("Procedimiento almacenado CARGAR_INVENTARIO_TEORICO ejecutado exitosamente");
                }

                // 3️⃣ Mover el archivo a una carpeta de respaldo
                var backupDir = Path.Combine(Path.GetDirectoryName(e.FullPath) ?? ".", "Procesados");
                if (!Directory.Exists(backupDir))
                    Directory.CreateDirectory(backupDir);

                var backupPath = Path.Combine(backupDir, Path.GetFileName(e.FullPath));
                File.Move(e.FullPath, backupPath, overwrite: true);
                Console.WriteLine($"Archivo movido a: {backupPath}");

                // 4️⃣ Notificar a todos los clientes (solo para inventario_teorico)
                if (string.Equals(modo, "inventario_teorico", StringComparison.OrdinalIgnoreCase))
                {
                    await _hubContext.Clients.All.SendAsync("InventarioActualizado");
                    Console.WriteLine("Inventario actualizado automáticamente por cambio en inventario_teorico.csv");
                }
            }
        }



        // Event handler cuando un archivo es eliminado
        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"Archivo eliminado: {e.FullPath}");

        }

        // Sobrecarga: recibe sólo la ruta (mantiene compatibilidad)
        private void MoveToRejected(string filePath)
        {
            MoveToRejected(filePath, "Rechazado");
        }

        // Nueva sobrecarga: recibe ruta + motivo (más útil para logging)
        private void MoveToRejected(string filePath, string reason)
        {
            try
            {
                var dir = Path.GetDirectoryName(filePath) ?? ".";
                var rejectDir = Path.Combine(dir, "Rechazados");

                if (!Directory.Exists(rejectDir))
                    Directory.CreateDirectory(rejectDir);

                var fileName = Path.GetFileName(filePath);
                var rejectPath = Path.Combine(rejectDir, fileName);

                // Mover archivo
                File.Move(filePath, rejectPath, overwrite: true);

                // Crear pequeño archivo de motivo para auditoría (opcional pero útil)
                try
                {
                    var motivoPath = Path.Combine(rejectDir, fileName + ".motivo.txt");
                    File.WriteAllText(motivoPath, $"{DateTime.UtcNow:O} - {reason}");
                }
                catch
                {
                    // si falla el escrito del motivo, no bloqueamos el flujo principal
                }

                Console.WriteLine($"Archivo rechazado movido a: {rejectPath}. Razon: {reason}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al mover archivo rechazado: {ex.Message}");
            }
        }



    }
}