using CatalystPMS.Core.Interfaces;
using CatalystPMS.Features.Approvals.Services;
using CatalystPMS.Features.AuditLogs.Services;
using CatalystPMS.Features.Auth.Services;
using CatalystPMS.Features.Categories.Services;
using CatalystPMS.Features.Notifications.Services;
using CatalystPMS.Features.Products.Services;
using CatalystPMS.Infrastructure.Data;
using CatalystPMS.Infrastructure.ExternalServices;
using CatalystPMS.Infrastructure.Identity;
using CatalystPMS.Infrastructure.Security;
using CatalystPMS.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
//builder.Services.AddDbContext<AppDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

// ── Identity ──────────────────────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ── JWT ───────────────────────────────────────────────────────────────────────
var azureAdSection = builder.Configuration.GetSection("AzureAd");
var tenantId = azureAdSection["TenantId"]
    ?? throw new InvalidOperationException("AzureAd:TenantId is missing from appsettings.json");
var apiClientId = azureAdSection["ClientId"]
    ?? throw new InvalidOperationException("AzureAd:ClientId is missing from appsettings.json");

builder.Services.AddAuthentication(options =>
{
options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => 
{

    options.MapInboundClaims = false;
    // Entra ID's OpenID Connect discovery document — ASP.NET Core fetches this
    // automatically and uses it to validate token signatures, issuer, etc.
    // You can view it yourself at:
    //   https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration
    options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";

        // The API's own App Registration client ID — this is the audience Entra ID
        // stamps into access tokens issued for calls to this API.
        options.Audience = apiClientId;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            //accepting both audience formats entra id might stamp into the token
            ValidAudiences = new[]
            {
                apiClientId,
                $"api://{apiClientId}"
            },

       
            RoleClaimType = "roles",
            NameClaimType = "name"
        };

    // TEMPORARY — keep this while debugging, remove once login works reliably.
    // Logs the actual reason a token was rejected, instead of a bare 401.
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine("─── JWT validation failed ───");
            Console.WriteLine(context.Exception.ToString());
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine("─── JWT challenge triggered ───");
            Console.WriteLine($"Error: {context.Error}, Description: {context.ErrorDescription}");
            return Task.CompletedTask;
        }
    };
});



builder.Services.AddAuthorization();
//var jwtSettings = builder.Configuration.GetSection("Jwt");
//builder.Services.Configure<JwtSettings>(jwtSettings);

//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
//})
//.AddJwtBearer(options =>
//{
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuer = true,
//        ValidateAudience = true,
//        ValidateLifetime = true,
//        ValidateIssuerSigningKey = true,
//        ValidIssuer = builder.Configuration["Jwt:Issuer"],
//        ValidAudience = builder.Configuration["Jwt:Audience"],
//        IssuerSigningKey = new SymmetricSecurityKey(
//        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]
//            ?? throw new InvalidOperationException("Jwt:Key is missing from appsettings.json")))
//    };
//});

//builder.Services.AddAuthorization();

// ── CORS (Angular cors server) ──────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularCors", policy => policy
        .WithOrigins("http://localhost:4200", "https://agreeable-flower-0aa0ffd0f.7.azurestaticapps.net")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// ── Application Services ───────────────────────────────────────────────────────
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDataLakeService, DataLakeService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// ── Controllers + Swagger ─────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Catalyst PMS API",
        Version = "v1",
        Description = "Product Management System — Catalyst Online Order Solution"
    });

    // Add JWT auth to Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// ── Build ─────────────────────────────────────────────────────────────────────
//var app = builder.Build();
//await DbSeeder.SeedAsync(app.Services);
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalyst PMS API v1"));
//}

//app.UseMiddleware<ExceptionMiddleware>();
//app.UseHttpsRedirection();
//app.UseCors("AngularCors");
//app.UseAuthentication();
//app.UseAuthorization();
//app.MapControllers();

//app.Run();
var app = builder.Build();
await DbSeeder.SeedAsync(app.Services);

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalyst PMS API v1"));

app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseCors("AngularCors");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();