using CatalystPMS.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalystPMS.Infrastructure.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.CategoryId);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.CreatedByUserId).IsRequired().HasMaxLength(450);
    }
}

public class ProductSpecificationConfiguration : IEntityTypeConfiguration<ProductSpecification>
{
    public void Configure(EntityTypeBuilder<ProductSpecification> builder)
    {
        builder.HasKey(s => s.SpecificationId);
        builder.Property(s => s.Key).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Value).IsRequired().HasMaxLength(500);

        builder.HasOne(s => s.Product)
            .WithMany(p => p.Specifications)
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ApprovalWorkflowConfiguration : IEntityTypeConfiguration<ApprovalWorkflow>
{
    public void Configure(EntityTypeBuilder<ApprovalWorkflow> builder)
    {
        builder.HasKey(a => a.WorkflowId);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(50);
        builder.Property(a => a.Comment).HasMaxLength(1000);
        builder.Property(a => a.ActorUserId).IsRequired().HasMaxLength(450);

        builder.HasOne(a => a.Product)
            .WithMany(p => p.ApprovalWorkflows)
            .HasForeignKey(a => a.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.LogId);
        builder.Property(a => a.ActionType).IsRequired().HasMaxLength(50);
        builder.Property(a => a.FieldName).HasMaxLength(100);
        builder.Property(a => a.OldValue).HasMaxLength(1000);
        builder.Property(a => a.NewValue).HasMaxLength(1000);
        builder.Property(a => a.ActorUserId).IsRequired().HasMaxLength(450);

        builder.HasOne(a => a.Product)
            .WithMany(p => p.AuditLogs)
            .HasForeignKey(a => a.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.NotificationId);
        builder.Property(n => n.Message).IsRequired().HasMaxLength(500);
        builder.Property(n => n.RecipientUserId).IsRequired().HasMaxLength(450);

        builder.HasOne(n => n.Product)
            .WithMany(p => p.Notifications)
            .HasForeignKey(n => n.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.ProductId);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Sku).IsRequired().HasMaxLength(50);
        builder.HasIndex(p => p.Sku).IsUnique();
        builder.Property(p => p.Brand).HasMaxLength(100);
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Property(p => p.ImageData).HasColumnType("varbinary(max)");
        builder.Property(p => p.ImageMimeType).HasMaxLength(50);
        builder.Property(p => p.CreatedByUserId).IsRequired().HasMaxLength(450);
        builder.Property(p => p.ApprovedByUserId).HasMaxLength(450);

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Status)
            .WithMany(s => s.Products)
            .HasForeignKey(p => p.StatusId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductStatusConfiguration : IEntityTypeConfiguration<ProductStatus>
{
    public void Configure(EntityTypeBuilder<ProductStatus> builder)
    {
        builder.HasKey(s => s.StatusId);
        builder.Property(s => s.StatusName).IsRequired().HasMaxLength(50);
    }
}
