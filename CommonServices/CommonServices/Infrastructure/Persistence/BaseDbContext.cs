using Microsoft.EntityFrameworkCore;
using SpreadsBack.CommonServices.Domain.Entities;

namespace SpreadsBack.CommonServices.Infrastructure.Persistence;

/// <summary>
/// DbContext base com configurações comuns
/// </summary>
public abstract class BaseDbContext : DbContext
{
    public DbSet<ProcessedEvent> ProcessedEvents { get; set; } = null!;
    public DbSet<FailedEvent> FailedEvents { get; set; } = null!;

    protected BaseDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuração comum para ProcessedEvent
        modelBuilder.Entity<ProcessedEvent>(entity =>
        {
            entity.ToTable("processed_events");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EventId).HasColumnName("event_id").IsRequired();
            entity.Property(e => e.EventType).HasColumnName("event_type").IsRequired();
            entity.Property(e => e.ProcessedAt).HasColumnName("processed_at").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();

            entity.HasIndex(e => e.EventId).IsUnique();
        });

        // Configuração comum para FailedEvent
        modelBuilder.Entity<FailedEvent>(entity =>
        {
            entity.ToTable("failed_events");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EventId).HasColumnName("event_id").IsRequired();
            entity.Property(e => e.EventType).HasColumnName("event_type").IsRequired();
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message").IsRequired();
            entity.Property(e => e.EventData).HasColumnName("event_data").IsRequired();
            entity.Property(e => e.RetryCount).HasColumnName("retry_count").IsRequired();
            entity.Property(e => e.FailedAt).HasColumnName("failed_at").IsRequired();
            entity.Property(e => e.LastRetryAt).HasColumnName("last_retry_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();

            entity.HasIndex(e => e.EventId);
        });

        // Aplicar configurações de entidades específicas
        ConfigureEntities(modelBuilder);
    }

    /// <summary>
    /// Configuração específica de entidades do microserviço
    /// </summary>
    protected abstract void ConfigureEntities(ModelBuilder modelBuilder);

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Atualizar UpdatedAt automaticamente
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
