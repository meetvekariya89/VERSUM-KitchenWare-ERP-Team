
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VERSUM.KitchenWareERP.Models;

namespace VERSUM.KitchenWareERP.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ==========================================
        // EXISTING VERSUM ERP DATABASE TABLES
        // ==========================================

        public DbSet<Category> Categories { get; set; } = null!;

        public DbSet<Product> Products { get; set; } = null!;

        public DbSet<RawMaterial> RawMaterials { get; set; } = null!;

        public DbSet<StockMovement> StockMovements { get; set; } = null!;

        public DbSet<ProductionBatch> ProductionBatches { get; set; } = null!;

        public DbSet<Dealer> Dealers { get; set; } = null!;

        public DbSet<Purchase> Purchases { get; set; } = null!;

        public DbSet<Sale> Sales { get; set; } = null!;

        public DbSet<SaleItem> SaleItems { get; set; } = null!;

        // ==========================================
        // DATABASE CONFIGURATION
        // ==========================================

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Required for ASP.NET Core Identity tables
            base.OnModelCreating(modelBuilder);

            // ======================================
            // UNIQUE INDEXES
            // ======================================

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.ProductCode)
                .IsUnique();

            modelBuilder.Entity<RawMaterial>()
                .HasIndex(r => r.MaterialCode)
                .IsUnique();

            modelBuilder.Entity<ProductionBatch>()
                .HasIndex(b => b.BatchNumber)
                .IsUnique();

            modelBuilder.Entity<Dealer>()
                .HasIndex(d => d.DealerCode)
                .IsUnique();

            modelBuilder.Entity<Purchase>()
                .HasIndex(p => p.PurchaseNumber)
                .IsUnique();

            modelBuilder.Entity<Sale>()
                .HasIndex(s => s.InvoiceNumber)
                .IsUnique();

            // ======================================
            // DATABASE RELATIONSHIPS
            // ======================================

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Purchase>()
                .HasOne(p => p.RawMaterial)
                .WithMany()
                .HasForeignKey(p => p.RawMaterialId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Sale>()
                .HasOne(s => s.Dealer)
                .WithMany()
                .HasForeignKey(s => s.DealerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleItem>()
                .HasOne(i => i.Sale)
                .WithMany(s => s.Items)
                .HasForeignKey(i => i.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SaleItem>()
                .HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
