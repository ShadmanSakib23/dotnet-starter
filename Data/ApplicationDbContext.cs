using Microsoft.EntityFrameworkCore;
using StarterApp.Models;

namespace StarterApp.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User entity configuration
        modelBuilder.Entity<User>(entity =>
        {
            // Indexes
            entity.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("idx_users_email");

            entity.HasIndex(u => u.Username)
                .IsUnique()
                .HasFilter("username IS NOT NULL")
                .HasDatabaseName("idx_users_username");

            // Database-generated values
            entity.Property(u => u.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(u => u.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(u => u.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
