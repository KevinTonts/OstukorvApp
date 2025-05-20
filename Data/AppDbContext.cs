using Microsoft.EntityFrameworkCore;
using OstukorvApp.Models; // Veendu, et Product klass on selles namespaces

namespace OstukorvApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Store> Stores { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>()
            .HasOne(p => p.Store)
            .WithMany(s => s.Products)
            .HasForeignKey(p => p.StoreId);

            modelBuilder.Entity<Product>()
                .Property(p => p.ScrapedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<Store>().HasData(
                new Store { Id = 1, Name = "Selver" },
                new Store { Id = 2, Name = "Coop" }
            );

        }
    }
}
