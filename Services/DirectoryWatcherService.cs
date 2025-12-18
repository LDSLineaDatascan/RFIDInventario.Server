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

                // 1️⃣ Procesar CSV
                fileSettings.Service?.CargaCsv(e.FullPath, fileSettings.FileSeparator, fileSettings.FileParams);

                // 2️⃣ Ejecutar el procedimiento almacenado
                await dbContext.Database.ExecuteSqlRawAsync("EXEC CARGAR_INVENTARIO_TEORICO");

                // 3️⃣ Mover el archivo a una carpeta de respaldo
                var backupDir = Path.Combine(Path.GetDirectoryName(e.FullPath), "Procesados");
                if (!Directory.Exists(backupDir))
                    Directory.CreateDirectory(backupDir);

                var backupPath = Path.Combine(backupDir, Path.GetFileName(e.FullPath));
                File.Move(e.FullPath, backupPath, overwrite: true);

                // 4️⃣ Notificar a todos los clientes
                await _hubContext.Clients.All.SendAsync("InventarioActualizado");
                Console.WriteLine($"Archivo movido a: {backupPath}");
                Console.WriteLine("Inventario actualizado automáticamente por cambio en inventario_teorico.csv");
            }
        }




        // Event handler cuando un archivo es eliminado
        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"Archivo eliminado: {e.FullPath}");
            
        }

    }
}
