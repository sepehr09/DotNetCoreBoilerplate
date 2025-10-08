using System.Text;
using Finbuckle.MultiTenant;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using MyApp.Application.Common.Interfaces;
using MyApp.Domain.Constants;
using MyApp.Domain.Entities;
using MyApp.Infrastructure.Data;
using MyApp.Infrastructure.Data.Interceptors;
using MyApp.Infrastructure.Identity;
using MyApp.Infrastructure.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("MyAppDb");
        Guard.Against.Null(connectionString, message: "Connection string 'MyAppDb' not found.");

        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(connectionString);
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        builder.Services.AddScoped<ITenantService, TenantService>();
        builder.Services.AddScoped<ApplicationDbContextInitialiser>();

        /* ---------------------------------- Redis --------------------------------- */
        var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "MyApp";
            });
        }

        /* ----------------------------- Authentication ----------------------------- */
        // Configure JWT Bearer authentication for single-tenant mode
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
                if (jwtSettings != null)
                {
                    options.RequireHttpsMetadata = builder.Environment.IsDevelopment(); // Disable HTTPS metadata requirement in development

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        ClockSkew = TimeSpan.Zero,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
                    };
                }
            });

        // Configure multi-tenancy
        if (builder.Configuration.GetValue<bool>("IsMultiTenant"))
        {
            if (builder.Environment.IsDevelopment())
            {
                builder.Services.AddMultiTenant<AppTenantInfo>()
                    .WithHeaderStrategy()
                    .WithDistributedCacheStore()
                    .WithPerTenantAuthentication();
            }
            else
            {
                builder.Services.AddMultiTenant<AppTenantInfo>()
                    .WithHostStrategy()
                    .WithDistributedCacheStore()
                    .WithPerTenantAuthentication();
            }

            // Configure JWT options per tenant
            builder.Services.ConfigurePerTenant<JwtBearerOptions, AppTenantInfo>(JwtBearerDefaults.AuthenticationScheme, (options, tenantInfo) =>
            {
                options.Authority = tenantInfo.JwtAuthority;
                options.RequireHttpsMetadata = false; // Always disable HTTPS metadata requirement for tenant authentication

                // Configure TokenValidationParameters per tenant
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidIssuer = tenantInfo.JwtIssuer ?? builder.Configuration["JwtSettings:Issuer"],
                    ValidAudience = tenantInfo.JwtAudience ?? builder.Configuration["JwtSettings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tenantInfo.JwtAuthority ?? builder.Configuration["JwtSettings:Secret"] ?? string.Empty))
                };
            });
        }


        /* ------------------------------ Authorization ----------------------------- */
        builder.Services.AddAuthorizationBuilder();

        builder.Services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddApiEndpoints();

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddTransient<IIdentityService, IdentityService>();
        builder.Services.AddTransient<IJwtTokenService, JwtTokenService>();

        builder.Services.AddAuthorization(options =>
            options.AddPolicy(Policies.CanPurge, policy => policy.RequireRole(Roles.Administrator)));


        // JWT settings
        builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

        // file storage services
        builder.Services.Configure<MinioSettings>(builder.Configuration.GetSection("MinIO"));
        builder.Services.AddScoped<IFileStorageService, MinioStorageService>();

        // distributed cache service
        builder.Services.AddScoped<IDistributedCacheService, DistributedCacheService>();
    }
}
