using FixFlow.Api.Domain.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixFlow.Api.Common.Persistence.Configurations;

public sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("clients");
        builder.HasKey(client => client.Id);
        builder.Property(client => client.Id).ValueGeneratedNever();
        builder.Property(client => client.Name).HasMaxLength(200);
        builder.Property(client => client.ContactPerson).HasMaxLength(200);
        builder.Property(client => client.Phone).HasMaxLength(20);
        builder.Property(client => client.Email).HasMaxLength(256);
        builder.ComplexProperty(client => client.Address, address =>
        {
            address.Property(value => value.Street).HasMaxLength(200);
            address.Property(value => value.BuildingNumber).HasMaxLength(20);
            address.Property(value => value.PostalCode).HasMaxLength(6);
            address.Property(value => value.City).HasMaxLength(100);
        });
        builder.HasIndex(client => client.Name);
    }
}
