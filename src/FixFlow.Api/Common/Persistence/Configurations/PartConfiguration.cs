using FixFlow.Api.Common.Concurrency;
using FixFlow.Api.Domain.Parts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixFlow.Api.Common.Persistence.Configurations;

public sealed class PartConfiguration : IEntityTypeConfiguration<Part>
{
    public const string CatalogNumberIndexName = "ix_parts_catalog_number";
    public const string StockQuantityCheckName = "ck_parts_stock_quantity_non_negative";

    public void Configure(EntityTypeBuilder<Part> builder)
    {
        builder.ToTable("parts", table => table.HasCheckConstraint(StockQuantityCheckName, "stock_quantity >= 0"));
        builder.HasKey(part => part.Id);
        builder.Property(part => part.Id).ValueGeneratedNever();
        builder.Property<uint>(EntityTag.VersionProperty).IsRowVersion();
        builder.Property(part => part.Name).HasMaxLength(200);
        builder.Property(part => part.CatalogNumber).HasMaxLength(50);
        builder.Property(part => part.UnitPrice).HasPrecision(12, 2);
        builder.HasIndex(part => part.CatalogNumber)
            .IsUnique()
            .HasDatabaseName(CatalogNumberIndexName);
    }
}
