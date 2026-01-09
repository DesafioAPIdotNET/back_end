using Microsoft.EntityFrameworkCore;
using AuthApi.Models;

namespace AuthApi.Data
{
    // O AppDbContext gerencia a conexão e tradução dos objetos para SQL
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Define a tabela "Products" no banco
        public DbSet<Product> Products { get; set; }
    }
}