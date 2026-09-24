using FixFlow.Api.Domain.Clients;
using FixFlow.Api.Domain.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixFlow.Api.Common.Persistence.Configurations;

public sealed class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public const string SerialNumberIndexName = "ix_devices_serial_number";

    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("devices");
        builder.HasKey(device => device.Id);
        builder.Property(device => device.Id).ValueGeneratedNever();
        builder.Property(device => device.SerialNumber).HasMaxLength(100);
        builder.Property(device => device.Model).HasMaxLength(100);
        builder.Property(device => device.Manufacturer).HasMaxLength(100);
        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(device => device.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(device => device.SerialNumber)
            .IsUnique()
            .HasDatabaseName(SerialNumberIndexName);
    }
}
