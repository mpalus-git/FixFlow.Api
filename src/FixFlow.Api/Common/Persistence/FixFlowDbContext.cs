using FixFlow.Api.Domain.Auth;
using FixFlow.Api.Domain.Clients;
using FixFlow.Api.Domain.Devices;
using FixFlow.Api.Domain.Parts;
using FixFlow.Api.Domain.ServiceEntries;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Common.Persistence;

public sealed class FixFlowDbContext(DbContextOptions<FixFlowDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Client> Clients => Set<Client>();

    public DbSet<Device> Devices => Set<Device>();

    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();

    public DbSet<Part> Parts => Set<Part>();

    public DbSet<ServiceEntry> ServiceEntries => Set<ServiceEntry>();

    public DbSet<WorkOrderNumberCounter> WorkOrderNumberCounters => Set<WorkOrderNumberCounter>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>().ToTable("users");
        builder.Entity<IdentityRole<Guid>>().ToTable("roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");

        builder.ApplyConfigurationsFromAssembly(typeof(FixFlowDbContext).Assembly);
    }
}
