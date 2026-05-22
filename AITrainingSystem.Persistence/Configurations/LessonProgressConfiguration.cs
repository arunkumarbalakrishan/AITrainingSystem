using AITrainingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AITrainingSystem.Persistence.Configurations;

public class LessonProgressConfiguration
    : IEntityTypeConfiguration<LessonProgress>
{
    public void Configure(EntityTypeBuilder<LessonProgress> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IsCompleted)
            .IsRequired();

        builder.Property(x => x.CompletedAt)
            .IsRequired();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId);

        builder.HasOne(x => x.Lesson)
            .WithMany(x => x.Progresses)
            .HasForeignKey(x => x.LessonId);

        builder.HasIndex(x => new
        {
            x.UserId,
            x.LessonId
        }).IsUnique();
    }
}