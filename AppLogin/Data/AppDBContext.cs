using Microsoft.EntityFrameworkCore;
using AppLogin.Models;
// < 
// >
namespace AppLogin.Data
{
    public class AppDBContext : DbContext{
        public AppDBContext(){}
        public AppDBContext(DbContextOptions<AppDBContext> options) : base(options){}
        public DbSet<Usuario> Usuarios{get;set;}

        public DbSet<UserAction> Actions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuario>(tb =>{
                tb.HasKey(col => col.IdUsuario);
                tb.Property(col => col.IdUsuario).UseIdentityColumn().ValueGeneratedOnAdd();
                tb.Property(col => col.NombreCompleto).HasMaxLength(50);
                tb.Property(col => col.Correo).HasMaxLength(50);
                tb.Property(col => col.Clave).HasMaxLength(50);
            });
            modelBuilder.Entity<UserAction>().ToTable("Usuario");

            modelBuilder.Entity<UserAction>(tb =>{
                tb.HasKey(col => col.Id);
                tb.Property(col => col.Id).UseIdentityColumn().ValueGeneratedOnAdd();
                tb.Property(col => col.UsuarioId).IsRequired();
                tb.Property(col => col.ActionName).HasMaxLength(100).IsRequired();
                tb.Property(col => col.UserAffected).IsRequired(false);
                tb.Property(col => col.IsDeleted).HasDefaultValue(false);
                tb.Property(col => col.CreatedAt).IsRequired();
            });
            modelBuilder.Entity<UserAction>().ToTable("UserAction");
        }
    }
}
