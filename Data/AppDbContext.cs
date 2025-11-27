using RFIDInventario.Server.Data;
using Microsoft.EntityFrameworkCore;
using RFIDInventario.Server.Models;

namespace RFIDInventario.Server.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Tienda> Tiendas { get; set; }
        public DbSet<Inventario> InventarioTeorico { get; set; }
        public DbSet<InventarioFisico> InventarioFisico { get; set; }
        public DbSet<TagTienda> TagTienda { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<UsuariosTiendas> UsuariosTiendas { get; set; }




        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Inventario>().HasKey(i => new {i.IdProducto, i.IdTienda});
            modelBuilder.Entity<InventarioFisico>().HasKey(i => new { i.IdProducto, i.IdTienda });
            modelBuilder.Entity<TagTienda>().HasKey(i => new { i.IdTienda, i.Tag });
        }
    }
}
