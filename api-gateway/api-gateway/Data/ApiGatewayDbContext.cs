using Microsoft.EntityFrameworkCore;
using api_gateway.Data.Models;

namespace api_gateway.Data;

public class ApiGatewayDbContext : DbContext
{
    public ApiGatewayDbContext(DbContextOptions<ApiGatewayDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<License> Licenses { get; set; }
    public DbSet<LicenseAuditLog> LicenseAuditLogs { get; set; }
    public DbSet<School> Schools { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Configure Role entity
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Configure UserRole relationship
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId);
            entity.HasOne(e => e.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(e => e.RoleId);
        });

        // Configure License entity
        modelBuilder.Entity<License>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LicenseKey).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LicenseType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.LicenseKey).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId);
        });

        // Configure LicenseAuditLog entity
        modelBuilder.Entity<LicenseAuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ChangedBy).IsRequired().HasMaxLength(100);
            entity.HasOne(e => e.License)
                .WithMany()
                .HasForeignKey(e => e.LicenseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure School entity
        modelBuilder.Entity<School>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.HasIndex(e => e.Code).IsUnique(false);
        });

        // Seed default roles
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Admin", Description = "Administrator role" },
            new Role { Id = 2, Name = "User", Description = "Standard user role" },
            new Role { Id = 3, Name = "Google", Description = "Google OAuth user" },
            new Role { Id = 4, Name = "Facebook", Description = "Facebook OAuth user" }
        );
    }
}
