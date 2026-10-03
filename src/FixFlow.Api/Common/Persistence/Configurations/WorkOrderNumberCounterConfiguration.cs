using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixFlow.Api.Common.Persistence.Configurations;

public sealed class WorkOrderNumberCounterConfiguration : IEntityTypeConfiguration<WorkOrderNumberCounter>
{
    public void Configure(EntityTypeBuilder<WorkOrderNumberCounter> builder)
    {
        builder.ToTable("work_order_number_counters");
        builder.HasKey(counter => counter.Year);
        builder.Property(counter => counter.Year).ValueGeneratedNever();
    }
}
