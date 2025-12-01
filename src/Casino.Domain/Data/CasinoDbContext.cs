using Casino.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Casino.Domain.Data;

public class CasinoDbContext : DbContext
{
    public CasinoDbContext(DbContextOptions<CasinoDbContext> options)
        : base(options)
    {
    }

    public DbSet<Wallet> Wallets { get; set; }
    public DbSet<Bet> Bets { get; set; }
    public DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Wallet configuration
        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TelegramId).IsUnique();
            entity.HasIndex(e => e.WalletAddress).IsUnique();
            entity.Property(e => e.TelegramId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.WalletAddress).IsRequired().HasMaxLength(44);
            entity.Property(e => e.EncryptedPrivateKey).IsRequired();
            entity.Property(e => e.Balance).HasPrecision(18, 9);
        });

        // Bet configuration
        modelBuilder.Entity<Bet>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Wallet)
                .WithMany(w => w.Bets)
                .HasForeignKey(e => e.WalletId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.TelegramId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Amount).HasPrecision(18, 9);
            entity.Property(e => e.WinAmount).HasPrecision(18, 9);
            entity.Property(e => e.GameType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TransactionHash).HasMaxLength(88);
        });

        // Transaction configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Wallet)
                .WithMany(w => w.Transactions)
                .HasForeignKey(e => e.WalletId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.TransactionHash).IsRequired().HasMaxLength(88);
            entity.Property(e => e.Amount).HasPrecision(18, 9);
            entity.Property(e => e.TransactionType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.TransactionHash);
        });
    }
}

