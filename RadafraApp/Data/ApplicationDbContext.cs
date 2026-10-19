using Microsoft.EntityFrameworkCore;
using RadafraApp.Entities;

namespace RadafraApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products => Set<Product>();

        public DbSet<Bid> Bids => Set<Bid>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(p => p.StartingPrice).HasPrecision(18, 2);
            });

            modelBuilder.Entity<Bid>(entity =>
            {
                entity.Property(b => b.Amount).HasPrecision(18, 2);

                entity.HasOne(b => b.Product)
                    .WithMany(p => p.Bids)
                    .HasForeignKey(b => b.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(b => b.ProductId);
            });
        }
    }
}
