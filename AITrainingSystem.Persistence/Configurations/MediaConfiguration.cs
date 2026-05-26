using AITrainingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AITrainingSystem.Infrastructure.Persistence.Configurations;

public class MediaConfiguration : IEntityTypeConfiguration<MediaFile>
{
    public void Configure(EntityTypeBuilder<MediaFile> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.StoredFileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.FilePath)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.FileType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.FileSize)
            .IsRequired();


        builder.Property(x => x.UploadedAt)
            .IsRequired();

        builder.Property(x => x.MediaType)
            .IsRequired();

        // Relationship
        builder.HasOne(x => x.Lesson)
            .WithMany(x => x.MediaFiles)
            .HasForeignKey(x => x.LessonId);
    }
}