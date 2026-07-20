using CatalystPMS.Core.Models;
using CatalystPMS.Infrastructure.Data;
using CatalystPMS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CatalystPMS.Infrastructure.Data;

/// <summary>
/// Seeds the database with lookup data (roles, product statuses), sample users,
/// categories, and a realistic spread of products across every workflow status.
/// Safe to run on every startup — it checks for existing data before inserting.
/// Call DbSeeder.SeedAsync(app.Services) once, right after `var app = builder.Build();`
/// and before `app.Run();` in Program.cs.
/// </summary>
public static class DbSeeder
{
    // Fixed IDs so the Angular status filter dropdown (which hardcodes 1-6) lines up exactly.
    private static readonly (int Id, string Name)[] Statuses =
    {
        (1, "Draft"),
        (2, "Pending Approval"),
        (3, "Approved"),
        (4, "Rejected"),
        (5, "Published"),
        (6, "Archived")
    };

    private static readonly string[] Roles = { "ProductCapturer", "ProductManager" };

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var context = provider.GetRequiredService<AppDbContext>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        await context.Database.MigrateAsync();

        await SeedRolesAsync(roleManager);
        await SeedProductStatusesAsync(context);

        // Only seed the rest if the database is empty — keeps this safe to run every startup.
        if (await context.Products.AnyAsync())
        {
            logger.LogInformation("DbSeeder: products already exist, skipping sample data seed.");
            return;
        }

        var users = await SeedUsersAsync(userManager);
        var categories = await SeedCategoriesAsync(context);
        await SeedProductsAsync(context, categories, users);

        logger.LogInformation("DbSeeder: sample data seeded successfully.");
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task SeedProductStatusesAsync(AppDbContext context)
    {
        foreach (var (id, name) in Statuses)
        {
            if (!await context.ProductStatuses.AnyAsync(s => s.StatusId == id))
            {
                context.ProductStatuses.Add(new ProductStatus { StatusId = id, StatusName = name });
            }
        }
        await context.SaveChangesAsync();
    }

    private class SeededUsers
    {
        public required ApplicationUser Manager1 { get; init; }
        public required ApplicationUser Manager2 { get; init; }
        public required ApplicationUser Capturer1 { get; init; }
        public required ApplicationUser Capturer2 { get; init; }
    }

    private static async Task<SeededUsers> SeedUsersAsync(UserManager<ApplicationUser> userManager)
    {
        async Task<ApplicationUser> CreateUser(string username, string email, string firstName, string lastName, string role)
        {
            var existing = await userManager.FindByNameAsync(username);
            if (existing != null) return existing;

            var user = new ApplicationUser
            {
                UserName = username,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName
            };

            // Sample-data password — meets the API's policy (upper, lower, digit, symbol, 8+ chars).
            // Change these before using this seeder against anything beyond local development.
            var result = await userManager.CreateAsync(user, "Sample@123");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to seed user '{username}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            await userManager.AddToRoleAsync(user, role);
            return user;
        }

        var manager1 = await CreateUser("tnkosi", "thabo.nkosi@catalystpms.local", "Thabo", "Nkosi", "ProductManager");
        var manager2 = await CreateUser("ldube", "lindiwe.dube@catalystpms.local", "Lindiwe", "Dube", "ProductManager");
        var capturer1 = await CreateUser("szulu", "sipho.zulu@catalystpms.local", "Sipho", "Zulu", "ProductCapturer");
        var capturer2 = await CreateUser("nmokoena", "naledi.mokoena@catalystpms.local", "Naledi", "Mokoena", "ProductCapturer");

        return new SeededUsers { Manager1 = manager1, Manager2 = manager2, Capturer1 = capturer1, Capturer2 = capturer2 };
    }

    private static async Task<List<Category>> SeedCategoriesAsync(AppDbContext context)
    {
        var categories = new List<Category>
        {
            new() { Name = "Electronics", Description = "Consumer electronics and accessories.", IsActive = true },
            new() { Name = "Home Appliances", Description = "Kitchen and household appliances.", IsActive = true },
            new() { Name = "Office Supplies", Description = "Stationery and office equipment.", IsActive = true },
            new() { Name = "Outdoor & Garden", Description = "Tools and equipment for outdoor spaces.", IsActive = true },
            new() { Name = "Health & Beauty", Description = "Personal care and wellness products.", IsActive = true }
        };

        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();
        return categories;
    }

    private static async Task SeedProductsAsync(AppDbContext context, List<Category> categories, SeededUsers users)
    {
        var electronics = categories.First(c => c.Name == "Electronics");
        var appliances = categories.First(c => c.Name == "Home Appliances");
        var office = categories.First(c => c.Name == "Office Supplies");
        var outdoor = categories.First(c => c.Name == "Outdoor & Garden");
        var beauty = categories.First(c => c.Name == "Health & Beauty");

        var now = DateTime.UtcNow;

        var products = new List<Product>
        {
            // 1. Draft — capturer1, never submitted
            new()
            {
                Name = "Wireless Mechanical Keyboard",
                Description = "Hot-swappable mechanical keyboard with wireless and USB-C connectivity.",
                Sku = "ELE-KEY-001",
                Brand = "Keystone",
                UnitPrice = 1299.00m,
                CategoryId = electronics.CategoryId,
                StatusId = 1, // Draft
                CreatedByUserId = users.Capturer1.Id,
                CreatedAt = now.AddDays(-2),
                UpdatedAt = now.AddDays(-2),
                Specifications = new List<ProductSpecification>
                {
                    new() { Key = "Switch type", Value = "Hot-swappable, brown tactile" },
                    new() { Key = "Connectivity", Value = "Bluetooth 5.0 / USB-C" },
                    new() { Key = "Battery life", Value = "Up to 200 hours" }
                }
            },

            // 2. Pending Approval — capturer2 submitted, awaiting manager review
            new()
            {
                Name = "Stainless Steel Air Fryer 6L",
                Description = "Large-capacity digital air fryer with 8 pre-set cooking modes.",
                Sku = "APP-AF-014",
                Brand = "Homeline",
                UnitPrice = 2199.00m,
                CategoryId = appliances.CategoryId,
                StatusId = 2, // Pending Approval
                CreatedByUserId = users.Capturer2.Id,
                CreatedAt = now.AddDays(-5),
                UpdatedAt = now.AddDays(-1),
                Specifications = new List<ProductSpecification>
                {
                    new() { Key = "Capacity", Value = "6 litres" },
                    new() { Key = "Power", Value = "1800W" },
                    new() { Key = "Presets", Value = "8 cooking modes" }
                },
                ApprovalWorkflows = new List<ApprovalWorkflow>
                {
                    new() { Action = "Submitted", ActorUserId = users.Capturer2.Id, ActionDate = now.AddDays(-1) }
                },
                AuditLogs = new List<AuditLog>
                {
                    new() { ActionType = "Created", ActorUserId = users.Capturer2.Id, LoggedAt = now.AddDays(-5) },
                    new() { ActionType = "StatusChanged", FieldName = "StatusId", OldValue = "Draft", NewValue = "Pending Approval", ActorUserId = users.Capturer2.Id, LoggedAt = now.AddDays(-1) }
                }
            },

            // 3. Approved — ready to publish
            new()
            {
                Name = "A4 Recycled Copy Paper (Box of 5 Reams)",
                Description = "80gsm recycled copy paper, suitable for laser and inkjet printers.",
                Sku = "OFF-PAP-002",
                Brand = "EcoPage",
                UnitPrice = 449.00m,
                CategoryId = office.CategoryId,
                StatusId = 3, // Approved
                CreatedByUserId = users.Capturer1.Id,
                ApprovedByUserId = users.Manager1.Id,
                CreatedAt = now.AddDays(-10),
                UpdatedAt = now.AddDays(-3),
                Specifications = new List<ProductSpecification>
                {
                    new() { Key = "Weight", Value = "80gsm" },
                    new() { Key = "Sheets per ream", Value = "500" },
                    new() { Key = "Recycled content", Value = "100%" }
                },
                ApprovalWorkflows = new List<ApprovalWorkflow>
                {
                    new() { Action = "Submitted", ActorUserId = users.Capturer1.Id, ActionDate = now.AddDays(-7) },
                    new() { Action = "Approved", Comment = "Looks good, pricing confirmed with supplier.", ActorUserId = users.Manager1.Id, ActionDate = now.AddDays(-3) }
                },
                AuditLogs = new List<AuditLog>
                {
                    new() { ActionType = "Created", ActorUserId = users.Capturer1.Id, LoggedAt = now.AddDays(-10) },
                    new() { ActionType = "StatusChanged", FieldName = "StatusId", OldValue = "Draft", NewValue = "Pending Approval", ActorUserId = users.Capturer1.Id, LoggedAt = now.AddDays(-7) },
                    new() { ActionType = "StatusChanged", FieldName = "StatusId", OldValue = "Pending Approval", NewValue = "Approved", ActorUserId = users.Manager1.Id, LoggedAt = now.AddDays(-3) }
                },
                Notifications = new List<Notification>
                {
                    new() { Message = "Your product 'A4 Recycled Copy Paper (Box of 5 Reams)' was approved.", RecipientUserId = users.Capturer1.Id, CreatedAt = now.AddDays(-3) }
                }
            },

            // 4. Rejected — sent back with a comment
            new()
            {
                Name = "Bluetooth Fitness Tracker Band",
                Description = "Heart-rate and sleep tracking fitness band with 10-day battery life.",
                Sku = "ELE-FIT-009",
                Brand = "PulseTrack",
                UnitPrice = 899.00m,
                CategoryId = electronics.CategoryId,
                StatusId = 4, // Rejected
                CreatedByUserId = users.Capturer2.Id,
                CreatedAt = now.AddDays(-6),
                UpdatedAt = now.AddDays(-2),
                Specifications = new List<ProductSpecification>
                {
                    new() { Key = "Battery life", Value = "10 days" },
                    new() { Key = "Water resistance", Value = "5 ATM" }
                },
                ApprovalWorkflows = new List<ApprovalWorkflow>
                {
                    new() { Action = "Submitted", ActorUserId = users.Capturer2.Id, ActionDate = now.AddDays(-4) },
                    new() { Action = "Rejected", Comment = "Missing warranty details and supplier certification. Please update and resubmit.", ActorUserId = users.Manager2.Id, ActionDate = now.AddDays(-2) }
                },
                AuditLogs = new List<AuditLog>
                {
                    new() { ActionType = "Created", ActorUserId = users.Capturer2.Id, LoggedAt = now.AddDays(-6) },
                    new() { ActionType = "StatusChanged", FieldName = "StatusId", OldValue = "Pending Approval", NewValue = "Rejected", ActorUserId = users.Manager2.Id, LoggedAt = now.AddDays(-2) }
                },
                Notifications = new List<Notification>
                {
                    new() { Message = "Your product 'Bluetooth Fitness Tracker Band' was rejected. See comments for details.", RecipientUserId = users.Capturer2.Id, CreatedAt = now.AddDays(-2) }
                }
            },

            // 5. Published — live in the catalog
            new()
            {
                Name = "Ergonomic Mesh Office Chair",
                Description = "Adjustable lumbar support, breathable mesh back, 3-year warranty.",
                Sku = "OFF-CHR-005",
                Brand = "SitRight",
                UnitPrice = 3499.00m,
                CategoryId = office.CategoryId,
                StatusId = 5, // Published
                CreatedByUserId = users.Capturer1.Id,
                ApprovedByUserId = users.Manager1.Id,
                CreatedAt = now.AddDays(-20),
                UpdatedAt = now.AddDays(-14),
                Specifications = new List<ProductSpecification>
                {
                    new() { Key = "Weight capacity", Value = "150kg" },
                    new() { Key = "Adjustability", Value = "Height, tilt, lumbar, armrests" },
                    new() { Key = "Warranty", Value = "3 years" }
                },
                ApprovalWorkflows = new List<ApprovalWorkflow>
                {
                    new() { Action = "Submitted", ActorUserId = users.Capturer1.Id, ActionDate = now.AddDays(-18) },
                    new() { Action = "Approved", Comment = "Solid product, matches supplier catalog.", ActorUserId = users.Manager1.Id, ActionDate = now.AddDays(-16) },
                    new() { Action = "Published", ActorUserId = users.Manager1.Id, ActionDate = now.AddDays(-14) }
                },
                AuditLogs = new List<AuditLog>
                {
                    new() { ActionType = "Created", ActorUserId = users.Capturer1.Id, LoggedAt = now.AddDays(-20) },
                    new() { ActionType = "StatusChanged", FieldName = "StatusId", OldValue = "Pending Approval", NewValue = "Approved", ActorUserId = users.Manager1.Id, LoggedAt = now.AddDays(-16) },
                    new() { ActionType = "StatusChanged", FieldName = "StatusId", OldValue = "Approved", NewValue = "Published", ActorUserId = users.Manager1.Id, LoggedAt = now.AddDays(-14) }
                }
            },

            // 6. Published — a second live product, different category
            new()
            {
                Name = "Galvanised Steel Garden Shed 3x2m",
                Description = "Weatherproof flat-pack garden shed with sliding door and ventilation.",
                Sku = "OUT-SHD-003",
                Brand = "YardWorks",
                UnitPrice = 6999.00m,
                CategoryId = outdoor.CategoryId,
                StatusId = 5, // Published
                CreatedByUserId = users.Capturer2.Id,
                ApprovedByUserId = users.Manager2.Id,
                CreatedAt = now.AddDays(-30),
                UpdatedAt = now.AddDays(-22),
                Specifications = new List<ProductSpecification>
                {
                    new() { Key = "Dimensions", Value = "3m x 2m x 2.1m" },
                    new() { Key = "Material", Value = "Galvanised steel" },
                    new() { Key = "Assembly", Value = "Flat-pack, tools included" }
                },
                ApprovalWorkflows = new List<ApprovalWorkflow>
                {
                    new() { Action = "Submitted", ActorUserId = users.Capturer2.Id, ActionDate = now.AddDays(-27) },
                    new() { Action = "Approved", ActorUserId = users.Manager2.Id, ActionDate = now.AddDays(-24) },
                    new() { Action = "Published", ActorUserId = users.Manager2.Id, ActionDate = now.AddDays(-22) }
                },
                AuditLogs = new List<AuditLog>
                {
                    new() { ActionType = "Created", ActorUserId = users.Capturer2.Id, LoggedAt = now.AddDays(-30) }
                }
            },

            // 7. Archived — previously published, now retired
            new()
            {
                Name = "Herbal Shampoo & Conditioner Set 400ml",
                Description = "Sulphate-free herbal hair care duo for daily use.",
                Sku = "HEA-SHM-011",
                Brand = "PureLeaf",
                UnitPrice = 259.00m,
                CategoryId = beauty.CategoryId,
                StatusId = 6, // Archived
                CreatedByUserId = users.Capturer1.Id,
                ApprovedByUserId = users.Manager1.Id,
                CreatedAt = now.AddDays(-90),
                UpdatedAt = now.AddDays(-5),
                Specifications = new List<ProductSpecification>
                {
                    new() { Key = "Volume", Value = "400ml each" },
                    new() { Key = "Formulation", Value = "Sulphate-free" }
                },
                ApprovalWorkflows = new List<ApprovalWorkflow>
                {
                    new() { Action = "Submitted", ActorUserId = users.Capturer1.Id, ActionDate = now.AddDays(-88) },
                    new() { Action = "Approved", ActorUserId = users.Manager1.Id, ActionDate = now.AddDays(-85) },
                    new() { Action = "Published", ActorUserId = users.Manager1.Id, ActionDate = now.AddDays(-84) },
                    new() { Action = "Unpublished", Comment = "Supplier discontinued this line.", ActorUserId = users.Manager1.Id, ActionDate = now.AddDays(-6) },
                    new() { Action = "Archived", ActorUserId = users.Manager1.Id, ActionDate = now.AddDays(-5) }
                },
                AuditLogs = new List<AuditLog>
                {
                    new() { ActionType = "Created", ActorUserId = users.Capturer1.Id, LoggedAt = now.AddDays(-90) },
                    new() { ActionType = "Archived", ActorUserId = users.Manager1.Id, LoggedAt = now.AddDays(-5) }
                }
            },

            // 8. Draft — a second draft for capturer2, no history yet
            new()
            {
                Name = "USB-C Docking Station 12-in-1",
                Description = "Multi-port docking station with dual HDMI, Ethernet, and 100W PD.",
                Sku = "ELE-DOC-021",
                Brand = "Keystone",
                UnitPrice = 1749.00m,
                CategoryId = electronics.CategoryId,
                StatusId = 1, // Draft
                CreatedByUserId = users.Capturer2.Id,
                CreatedAt = now.AddHours(-6),
                UpdatedAt = now.AddHours(-6),
                Specifications = new List<ProductSpecification>
                {
                    new() { Key = "Ports", Value = "2x HDMI, 3x USB-A, 2x USB-C, Ethernet, SD card" },
                    new() { Key = "Power delivery", Value = "100W" }
                }
            }
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }
}