using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StarterApp.Models;
using StarterApp.Enums;

namespace StarterApp.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(t => t.Type)
            .HasConversion<string>();

        builder.Property(t => t.Amount)
            .HasPrecision(18, 2);

        builder.Property(t => t.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(t => t.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(t => t.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(t => t.UserId)
            .HasDatabaseName("idx_transactions_user_id");

        builder.HasIndex(t => t.Type)
            .HasDatabaseName("idx_transactions_type");

        builder.HasIndex(t => t.Category)
            .HasDatabaseName("idx_transactions_category");

        builder.HasIndex(t => t.ProcessedAt)
            .HasDatabaseName("idx_transactions_processed_at");

        builder.HasIndex(t => t.CreatedAt)
            .HasDatabaseName("idx_transactions_created_at");

        builder.HasIndex(t => new { t.UserId, t.ProcessedAt })
            .HasDatabaseName("idx_transactions_user_processed");

        builder.HasIndex(t => new { t.UserId, t.Type })
            .HasDatabaseName("idx_transactions_user_type");

        builder.HasIndex(t => new { t.UserId, t.Category })
            .HasDatabaseName("idx_transactions_user_category");

        builder.HasIndex(t => new { t.UserId, t.Type, t.ProcessedAt })
            .HasDatabaseName("idx_transactions_user_type_processed");
    }
}
