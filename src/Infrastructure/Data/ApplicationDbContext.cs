using System.Reflection;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyApp.Application.Common.Interfaces;
using MyApp.Domain.Entities;
using MyApp.Infrastructure.Identity;

namespace MyApp.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext, IMultiTenantDbContext
{
    public ITenantInfo? TenantInfo { get; }
    public TenantMismatchMode TenantMismatchMode { get; } = TenantMismatchMode.Throw;
    public TenantNotSetMode TenantNotSetMode { get; } = TenantNotSetMode.Throw;
    private readonly IConfiguration? _configuration;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IMultiTenantContextAccessor<AppTenantInfo>? multiTenantContextAccessor = null, IConfiguration? configuration = null) : base(options)
    {
        _configuration = configuration;
        if (_configuration?.GetValue<bool>("IsMultiTenant") == true)
        {
            TenantInfo = multiTenantContextAccessor?.MultiTenantContext?.TenantInfo;
        }
    }


    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TodoList> TodoLists => Set<TodoList>();
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }


    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        // Only enforce multi-tenancy if it's enabled
        if (_configuration?.GetValue<bool>("IsMultiTenant") == true)
        {
            this.EnforceMultiTenant();
        }
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        // Only enforce multi-tenancy if it's enabled
        if (_configuration?.GetValue<bool>("IsMultiTenant") == true)
        {
            this.EnforceMultiTenant();
        }
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

}
