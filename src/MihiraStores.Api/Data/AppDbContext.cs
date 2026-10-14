namespace MihiraStores.Api.Data;

using Microsoft.EntityFrameworkCore;
using MihiraStores.Api.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferLine> StockTransferLines => Set<StockTransferLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Branch
        modelBuilder.Entity<Branch>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.Property(x => x.Code).HasMaxLength(20).IsRequired();
            b.HasIndex(x => x.Code).IsUnique();
        });

        // Product
        modelBuilder.Entity<Product>(p =>
        {
            p.HasKey(x => x.Id);
            p.Property(x => x.Name).HasMaxLength(150).IsRequired();
            p.Property(x => x.SKU).HasMaxLength(50).IsRequired();
            p.HasIndex(x => x.SKU).IsUnique();
        });

        // Stock
        modelBuilder.Entity<Stock>(s =>
        {
            s.HasKey(x => x.Id);
            s.Property(x => x.LastUpdatedUtc).IsConcurrencyToken();
            s.HasIndex(x => new { x.BranchId, x.ProductId }).IsUnique();

            s.HasOne(x => x.Branch)
                .WithMany(b => b.Stocks)
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            s.HasOne(x => x.Product)
                .WithMany(p => p.Stocks)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            s.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Stock_Quantity_NonNegative", "[Quantity] >= 0");
            });
        });

        // StockTransfer
        modelBuilder.Entity<StockTransfer>(st =>
        {
            st.HasKey(x => x.Id);
            st.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired()
                .IsConcurrencyToken();

            st.HasOne(x => x.FromBranch)
                .WithMany(b => b.OutgoingTransfers)
                .HasForeignKey(x => x.FromBranchId)
                .OnDelete(DeleteBehavior.Restrict);

            st.HasOne(x => x.ToBranch)
                .WithMany(b => b.IncomingTransfers)
                .HasForeignKey(x => x.ToBranchId)
                .OnDelete(DeleteBehavior.Restrict);

            st.HasMany(x => x.Lines)
                .WithOne(l => l.StockTransfer)
                .HasForeignKey(l => l.StockTransferId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // StockTransferLine
        modelBuilder.Entity<StockTransferLine>(stl =>
        {
            stl.HasKey(x => x.Id);

            stl.HasOne(x => x.Product)
                .WithMany(p => p.TransferLines)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            stl.ToTable(t =>
            {
                t.HasCheckConstraint("CK_StockTransferLine_Quantity_Positive", "[Quantity] > 0");
            });
        });
    }
}
