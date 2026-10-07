using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixFlow.Api.Common.Persistence.Configurations;

public sealed class WorkOrderEventConfiguration : IEntityTypeConfiguration<WorkOrderEvent>
{
    public void Configure(EntityTypeBuilder<WorkOrderEvent> builder)
    {
        builder.ToTable("work_order_events");
        builder.HasKey(workOrderEvent => workOrderEvent.Id);
        builder.Property(workOrderEvent => workOrderEvent.Id).ValueGeneratedNever();
        builder.Property(workOrderEvent => workOrderEvent.Type).HasConversion<string>().HasMaxLength(20);
        builder.HasOne<WorkOrder>()
            .WithMany()
            .HasForeignKey(workOrderEvent => workOrderEvent.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(workOrderEvent => workOrderEvent.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(workOrderEvent => workOrderEvent.TechnicianId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(workOrderEvent => new { workOrderEvent.WorkOrderId, workOrderEvent.OccurredAt });
    }
}
