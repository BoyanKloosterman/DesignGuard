using DesignGuard.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DesignGuard.Data;

public sealed class DesignGuardDbContext : DbContext
{
    public DesignGuardDbContext(DbContextOptions<DesignGuardDbContext> options)
        : base(options)
    {
    }

    public DbSet<ProjectEntity> Projects => Set<ProjectEntity>();
    public DbSet<TrustBoundaryEntity> TrustBoundaries => Set<TrustBoundaryEntity>();
    public DbSet<ComponentEntity> Components => Set<ComponentEntity>();
    public DbSet<DataFlowEntity> DataFlows => Set<DataFlowEntity>();
    public DbSet<UserRoleEntity> UserRoles => Set<UserRoleEntity>();
    public DbSet<AssetEntity> Assets => Set<AssetEntity>();
    public DbSet<DesignNoteEntity> DesignNotes => Set<DesignNoteEntity>();
    public DbSet<ControlEntity> Controls => Set<ControlEntity>();
    public DbSet<EntryPointEntity> EntryPoints => Set<EntryPointEntity>();
    public DbSet<SensitiveDataEntity> SensitiveDataItems => Set<SensitiveDataEntity>();
    public DbSet<ReviewItemEntity> ReviewItems => Set<ReviewItemEntity>();
    public DbSet<SnapshotEntity> Snapshots => Set<SnapshotEntity>();
    public DbSet<ThreatEntity> Threats => Set<ThreatEntity>();
    public DbSet<RequirementEntity> Requirements => Set<RequirementEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var project = modelBuilder.Entity<ProjectEntity>();
        project.HasKey(p => p.Id);

        project.HasMany(p => p.TrustBoundaries)
            .WithOne(t => t.Project!)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        project.HasMany(p => p.Components)
            .WithOne(c => c.Project!)
            .HasForeignKey(c => c.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        project.HasMany(p => p.DataFlows)
            .WithOne(f => f.Project!)
            .HasForeignKey(f => f.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        project.HasMany(p => p.UserRoles)
            .WithOne(r => r.Project!)
            .HasForeignKey(r => r.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        project.HasMany(p => p.Assets)
            .WithOne(a => a.Project!)
            .HasForeignKey(a => a.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        project.HasMany(p => p.DesignNotes)
            .WithOne(n => n.Project!)
            .HasForeignKey(n => n.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        project.HasMany(p => p.Controls)
            .WithOne(c => c.Project!)
            .HasForeignKey(c => c.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        project.HasMany(p => p.EntryPoints)
            .WithOne(x => x.Project!)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        project.HasMany(p => p.SensitiveDataItems)
            .WithOne(x => x.Project!)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        project.HasMany(p => p.ReviewItems)
            .WithOne(x => x.Project!)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        project.HasMany(p => p.Snapshots)
            .WithOne(x => x.Project!)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        project.HasMany(p => p.Threats)
            .WithOne(t => t.Project!)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        project.HasMany(p => p.Requirements)
            .WithOne(r => r.Project!)
            .HasForeignKey(r => r.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TrustBoundaryEntity>().HasKey(t => t.Id);

        var comp = modelBuilder.Entity<ComponentEntity>();
        comp.HasKey(c => c.Id);
        comp.HasOne(c => c.TrustBoundary)
            .WithMany()
            .HasForeignKey(c => c.TrustBoundaryId)
            .OnDelete(DeleteBehavior.SetNull);

        var flow = modelBuilder.Entity<DataFlowEntity>();
        flow.HasKey(f => f.Id);
        flow.HasOne(f => f.FromComponent)
            .WithMany()
            .HasForeignKey(f => f.FromComponentId)
            .OnDelete(DeleteBehavior.Restrict);
        flow.HasOne(f => f.ToComponent)
            .WithMany()
            .HasForeignKey(f => f.ToComponentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserRoleEntity>().HasKey(r => r.Id);
        modelBuilder.Entity<AssetEntity>().HasKey(a => a.Id);
        modelBuilder.Entity<DesignNoteEntity>().HasKey(n => n.Id);
        modelBuilder.Entity<ControlEntity>().HasKey(c => c.Id);
        modelBuilder.Entity<EntryPointEntity>().HasKey(x => x.Id);
        modelBuilder.Entity<SensitiveDataEntity>().HasKey(x => x.Id);
        modelBuilder.Entity<ReviewItemEntity>().HasKey(x => x.Id);
        modelBuilder.Entity<SnapshotEntity>().HasKey(x => x.Id);
        modelBuilder.Entity<ThreatEntity>().HasKey(t => t.Id);
        modelBuilder.Entity<RequirementEntity>().HasKey(r => r.Id);
    }
}
