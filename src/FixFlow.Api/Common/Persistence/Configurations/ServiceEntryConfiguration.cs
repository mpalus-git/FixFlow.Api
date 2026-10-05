using FixFlow.Api.Domain.Parts;
using FixFlow.Api.Domain.ServiceEntries;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixFlow.Api.Common.Persistence.Configurations;

public sealed class ServiceEntryConfiguration : IEntityTypeConfiguration<ServiceEntry>
{
    public const string PrimaryKeyName = "pk_service_entries";

    public void Configure(EntityTypeBuilder<ServiceEntry> builder)
    {
        builder.ToTable("service_entries");
        builder.HasKey(entry => entry.Id).HasName(PrimaryKeyName);
        builder.Property(entry => entry.Id).ValueGeneratedNever();
        builder.Property(entry => entry.Note).HasMaxLength(4000);
        builder.PrimitiveCollection(entry => entry.PhotoUrls);
        builder.HasOne<WorkOrder>()
            .WithMany()
            .HasForeignKey(entry => entry.WorkOrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(entry => entry.TechnicianId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.OwnsMany(entry => entry.Parts, parts =>
        {
            parts.ToTable("service_entry_parts", table => table.HasCheckConstraint("ck_service_entry_parts_quantity_positive", "quantity > 0"));
            parts.WithOwner().HasForeignKey("ServiceEntryId");
            parts.HasKey("ServiceEntryId", nameof(ServiceEntryPart.PartId));
            parts.Property(part => part.UnitPrice).HasPrecision(12, 2);
            parts.HasOne<Part>()
                .WithMany()
                .HasForeignKey(part => part.PartId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
