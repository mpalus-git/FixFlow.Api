using FixFlow.Api.Domain.Photos;
using FixFlow.Api.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixFlow.Api.Common.Persistence.Configurations;

public sealed class PhotoConfiguration : IEntityTypeConfiguration<Photo>
{
    public const string PrimaryKeyName = "pk_photos";

    public void Configure(EntityTypeBuilder<Photo> builder)
    {
        builder.ToTable("photos");
        builder.HasKey(photo => photo.Id).HasName(PrimaryKeyName);
        builder.Property(photo => photo.Id).ValueGeneratedNever();
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(photo => photo.TechnicianId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(photo => new { photo.TechnicianId, photo.UploadedAt });
    }
}
