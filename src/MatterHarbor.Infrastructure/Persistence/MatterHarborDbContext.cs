using Microsoft.EntityFrameworkCore;
using MatterHarbor.Domain.Auditing;
using MatterHarbor.Domain.Cases;
using MatterHarbor.Domain.Organizations;

namespace MatterHarbor.Infrastructure.Persistence;

public sealed class MatterHarborDbContext(DbContextOptions<MatterHarborDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<OrganizationUser> OrganizationUsers => Set<OrganizationUser>();

    public DbSet<CaseItem> Cases => Set<CaseItem>();

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("matterharbor");

        modelBuilder.Entity<Organization>(builder =>
        {
            builder.ToTable("organizations");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<OrganizationUser>(builder =>
        {
            builder.ToTable("organization_users");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ExternalSubject).HasMaxLength(200).IsRequired();
            builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            builder.HasIndex(x => new { x.OrganizationId, x.ExternalSubject }).IsUnique();
            builder.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CaseItem>(builder =>
        {
            builder.ToTable("cases");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.CaseNumber).HasMaxLength(40).IsRequired();
            builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(4_000).IsRequired();
            builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.Version).IsConcurrencyToken();
            builder.HasIndex(x => new { x.OrganizationId, x.CaseNumber }).IsUnique();
            builder.HasIndex(x => new { x.OrganizationId, x.CreatedAt });
            builder.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<OrganizationUser>().WithMany().HasForeignKey(x => x.AssignedUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditEntry>(builder =>
        {
            builder.ToTable("audit_entries");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
            builder.HasIndex(x => new { x.OrganizationId, x.EntityId, x.OccurredAt });
        });

        modelBuilder.Entity<IdempotencyRecord>(builder =>
        {
            builder.ToTable("idempotency_records");
            builder.HasKey(x => new { x.OrganizationId, x.Key });
            builder.Property(x => x.Key).HasMaxLength(200);
            builder.Property(x => x.RequestHash).HasMaxLength(64);
            builder.Property(x => x.ResponseJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("outbox_messages");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Type).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Payload).HasColumnType("jsonb");
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.LastErrorCode).HasMaxLength(100);
            builder.HasIndex(x => new { x.Status, x.LockedUntil, x.OccurredAt });
            builder.HasIndex(x => new { x.OrganizationId, x.OccurredAt });
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<AuditEntry>())
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException("Audit entries are immutable.");
            }
        }

        foreach (var entry in ChangeTracker.Entries<CaseItem>().Where(x => x.State == EntityState.Modified))
        {
            var version = entry.Property(x => x.Version);
            version.CurrentValue = version.OriginalValue + 1;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
