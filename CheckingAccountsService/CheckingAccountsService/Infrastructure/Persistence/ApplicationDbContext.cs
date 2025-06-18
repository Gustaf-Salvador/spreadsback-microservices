using CheckingAccountsService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using SpreadsBack.CommonServices.Infrastructure.Persistence;

namespace CheckingAccountsService.Infrastructure.Persistence;

public class ApplicationDbContext : BaseDbContext
{
    public DbSet<CheckingAccount> CheckingAccounts { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<WithdrawalLimit> WithdrawalLimits { get; set; } = null!;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void ConfigureEntities(ModelBuilder modelBuilder)
    {
        // Configure CheckingAccount entity
        modelBuilder.Entity<CheckingAccount>(entity =>
        {
            entity.ToTable("checking_accounts");

            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(e => e.CurrencyId).HasColumnName("currency_id").IsRequired();
            entity.Property(e => e.Balance).HasColumnName("balance").IsRequired();

            // Create a unique constraint on UserId and CurrencyId
            entity.HasIndex(e => new { e.UserId, e.CurrencyId }).IsUnique();
        });

        // Configure Transaction entity
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("transactions");

            entity.Property(e => e.CheckingAccountId).HasColumnName("checking_account_id").IsRequired();
            entity.Property(e => e.Amount).HasColumnName("amount").IsRequired();
            entity.Property(e => e.Type).HasColumnName("type")
                .HasConversion(
                    v => v.ToString(),
                    v => (TransactionType)Enum.Parse(typeof(TransactionType), v)
                )
                .IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");

            // Define relationship with CheckingAccount
            entity.HasOne<CheckingAccount>()
                .WithMany()
                .HasForeignKey(e => e.CheckingAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure WithdrawalLimit entity
        modelBuilder.Entity<WithdrawalLimit>(entity =>
        {
            entity.ToTable("withdrawal_limits");

            entity.Property(e => e.DailyLimit).HasColumnName("daily_limit").IsRequired();
            entity.Property(e => e.MonthlyLimit).HasColumnName("monthly_limit").IsRequired();

            // Create a unique constraint on UserId and CurrencyId
            entity.HasIndex(e => new { e.UserId, e.CurrencyId }).IsUnique();
        });
    }
}