using FixFlow.Api.Common.Concurrency;
using FixFlow.Api.Domain.Devices;
using FixFlow.Api.Domain.Photos;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixFlow.Api.Common.Persistence.Configurations;

public sealed class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public const string TechnicianInProgressIndexName = "ix_work_orders_technician_id_in_progress";

    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.ToTable("work_orders");
        builder.HasKey(workOrder => workOrder.Id);
        builder.Property(workOrder => workOrder.Id).ValueGeneratedNever();
        builder.Property<uint>(EntityTag.VersionProperty).IsRowVersion();
        builder.Ignore(workOrder => workOrder.PendingEvents);
        builder.Property(workOrder => workOrder.Number).HasMaxLength(WorkOrderNumber.MaxLength);
        builder.HasIndex(workOrder => workOrder.Number).IsUnique();
        builder.Property(workOrder => workOrder.Description).HasMaxLength(2000);
        builder.Property(workOrder => workOrder.Priority).HasConversion<string>().HasMaxLength(20);
        builder.Property(workOrder => workOrder.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasOne<Device>()
            .WithMany()
            .HasForeignKey(workOrder => workOrder.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(workOrder => workOrder.TechnicianId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(workOrder => workOrder.TechnicianId);
        builder.HasOne<Photo>()
            .WithMany()
            .HasForeignKey(workOrder => workOrder.ClientSignaturePhotoId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(workOrder => new { workOrder.DueDate, workOrder.Id });
        builder.HasIndex(workOrder => workOrder.TechnicianId, TechnicianInProgressIndexName)
            .HasDatabaseName(TechnicianInProgressIndexName)
            .IsUnique()
            .HasFilter($"status = '{nameof(WorkOrderStatus.InProgress)}'");
    }
}
