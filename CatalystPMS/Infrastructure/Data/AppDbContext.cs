using CatalystPMS.Core.Enums;
using CatalystPMS.Core.Models;
using CatalystPMS.Infrastructure.Data.Configurations;
using CatalystPMS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CatalystPMS.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductStatus> ProductStatuses => Set<ProductStatus>();
    public DbSet<ProductSpecification> ProductSpecifications => Set<ProductSpecification>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ApprovalWorkflow> ApprovalWorkflows => Set<ApprovalWorkflow>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // must be first — sets up Identity tables

        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new ProductSpecificationConfiguration());
        modelBuilder.ApplyConfiguration(new ApprovalWorkflowConfiguration());
        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new NotificationConfiguration());
        modelBuilder.ApplyConfiguration(new ProductStatusConfiguration());

        SeedRoles(modelBuilder);
        SeedProductStatuses(modelBuilder);


        //index for lookups when explicitly querying deleted products
        //used by receycle bin endpoints which call ignoreQueryFilters() to get deleted products
        modelBuilder.Entity<Product>()
            .HasQueryFilter(p => !p.IsDeleted);
    }

    //private static void SeedRoles(ModelBuilder modelBuilder)
    //{
    //    modelBuilder.Entity<IdentityRole>().HasData(
    //        new IdentityRole
    //        {
    //            Id = "role-capturer",
    //            Name = UserRoles.ProductCapturer,
    //            NormalizedName = UserRoles.ProductCapturer.ToUpper(),
    //            ConcurrencyStamp = "1"
    //        },
    //        new IdentityRole
    //        {
    //            Id = "role-manager",
    //            Name = UserRoles.ProductManager,
    //            NormalizedName = UserRoles.ProductManager.ToUpper(),
    //            ConcurrencyStamp = "2"
    //        }
    //    );
    //}

    private static void SeedRoles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdentityRole>().HasData(
            new IdentityRole
            {
                Id = "role-capturer",
                Name = "ProductCapturer",
                NormalizedName = "PRODUCTCAPTURER",
                ConcurrencyStamp = "1"
            },
            new IdentityRole
            {
                Id = "role-manager",
                Name = "ProductManager",
                NormalizedName = "PRODUCTMANAGER",
                ConcurrencyStamp = "2"
            }
        );
    }

    private static void SeedProductStatuses(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductStatus>().HasData(
            new ProductStatus { StatusId = (int)ProductStatusEnum.Draft, StatusName = "Draft" },
            new ProductStatus { StatusId = (int)ProductStatusEnum.PendingApproval, StatusName = "Pending Approval" },
            new ProductStatus { StatusId = (int)ProductStatusEnum.Approved, StatusName = "Approved" },
            new ProductStatus { StatusId = (int)ProductStatusEnum.Rejected, StatusName = "Rejected" },
            new ProductStatus { StatusId = (int)ProductStatusEnum.Active, StatusName = "Active" },
            new ProductStatus { StatusId = (int)ProductStatusEnum.Inactive, StatusName = "Inactive" },
            new ProductStatus { StatusId = (int)ProductStatusEnum.Archived, StatusName = "Archived" }
        );
    }
}