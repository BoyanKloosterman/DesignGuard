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
    public DbSet<ComponentEntity> Components => Set<ComponentEntity>();
    public DbSet<DataFlowEntity> DataFlows => Set<DataFlowEntity>();
    public DbSet<UserRoleEntity> UserRoles => Set<UserRoleEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var project = modelBuilder.Entity<ProjectEntity>();
        project.HasKey(p => p.Id);
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

        modelBuilder.Entity<ComponentEntity>().HasKey(c => c.Id);
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
    }
}
