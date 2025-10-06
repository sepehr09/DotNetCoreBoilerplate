using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.AspNetCore;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
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

        /* -------------------------- Finbuckle.MultiTenant ------------------------- */
        builder.Services.AddMultiTenant<AppTenantInfo>()
            // .WithHostStrategy()
            .WithStaticStrategy("TestTenant")
            .WithInMemoryStore(options =>
            {
                options.IsCaseSensitive = true;
                options.Tenants.Add(new AppTenantInfo { Identifier = "TestTenant", Id = "12345", Name = "Test tenant" });
            });
        // .WithDistributedCacheStore();

        /* ----------------------------- Authentication ----------------------------- */
        builder.Services.AddAuthentication()
            .AddBearerToken(IdentityConstants.BearerScheme);

        /* ------------------------------ Authorization ----------------------------- */
        builder.Services.AddAuthorizationBuilder();

        builder.Services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddApiEndpoints();

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddTransient<IIdentityService, IdentityService>();

        builder.Services.AddAuthorization(options =>
            options.AddPolicy(Policies.CanPurge, policy => policy.RequireRole(Roles.Administrator)));

        // file storage services
        builder.Services.Configure<MinioSettings>(builder.Configuration.GetSection("MinIO"));
        builder.Services.AddScoped<IFileStorageService, MinioStorageService>();

        // distributed cache service
        builder.Services.AddScoped<IDistributedCacheService, DistributedCacheService>();
    }
}
