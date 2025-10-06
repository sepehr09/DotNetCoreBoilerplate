using System.Reflection;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyApp.Application.Common.Exceptions;
using MyApp.Application.Common.Interfaces;
using MyApp.Domain.Entities;
using MyApp.Infrastructure.Identity;

namespace MyApp.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext, IMultiTenantDbContext
{
    public ITenantInfo? TenantInfo { get; }
    public TenantMismatchMode TenantMismatchMode { get; } = TenantMismatchMode.Throw;
    public TenantNotSetMode TenantNotSetMode { get; } = TenantNotSetMode.Throw;


    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IMultiTenantContextAccessor<AppTenantInfo>? multiTenantContextAccessor = null) : base(options)
    {
        TenantInfo = multiTenantContextAccessor?.MultiTenantContext?.TenantInfo;
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
        this.EnforceMultiTenant();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        this.EnforceMultiTenant();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

}
