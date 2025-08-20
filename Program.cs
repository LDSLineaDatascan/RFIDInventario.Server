using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RFIDInventario.Server.Data;
using RFIDInventario.Server.Hubs;
using RFIDInventario.Server.Models;
using RFIDInventario.Server.Services;

var builder = WebApplication.CreateBuilder(args);


//**************************** CONFIGURACIÓN PERSONALIZADA ****************************//

// 1. Cargar configuración desde appsettings.json
var fileParamsSettings = builder.Configuration
    .GetSection("FileParams")
    .Get<FileParams.FileParamsSettings>();

// 2. Registrar configuración como Singleton
builder.Services.AddSingleton(fileParamsSettings);

// 3. Registrar DirectoryWatcherService como Singleton con inyección de dependencias
builder.Services.AddSingleton(provider =>
{
    var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
    var hubContext = provider.GetRequiredService<IHubContext<NotificationHub>>();
    return new DirectoryWatcherService(scopeFactory, fileParamsSettings, hubContext);
});

//**************************** REGISTRO DE SERVICIOS ****************************//

//descoment5ada pra probar signalr reinicio
builder.Services.AddScoped<InventarioService>();
builder.Services.AddScoped<ICarga, InventarioService>();
builder.Services.AddScoped<ProductoService>();
builder.Services.AddScoped<TiendaService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();




//Signal R hosted
builder.Services.AddSignalR();
builder.Services.AddHostedService<DataChangeService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",
                "https://localhost:4200",
                "http://localhost:80"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // permitir el uso de cookies
    });
});

//**************************** CONFIGURACIÓN DE LA APLICACIÓN ****************************//

var app = builder.Build();

//NotificationHub corriendo
app.MapHub<NotificationHub>("/notificacionInventarios");
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("AllowAllOrigins");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//  inicia automaticamente la escucha del directorio cuando la aplicación se inicia
using (var scope = app.Services.CreateScope())
{
    var directoryWatcherService = scope.ServiceProvider.GetRequiredService<DirectoryWatcherService>();
    directoryWatcherService.StartListening();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
