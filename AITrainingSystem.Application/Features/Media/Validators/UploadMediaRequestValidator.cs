using FluentValidation;
using AITrainingSystem.Application.Features.Media.DTOs;

namespace AITrainingSystem.Application.Features.Media.Validators;

public class UploadMediaRequestValidator
    : AbstractValidator<UploadMediaRequestDto>
{
    public UploadMediaRequestValidator()
    {
        RuleFor(x => x.File)
            .NotNull()
            .WithMessage("File is required");

        RuleFor(x => x.LessonId)
            .NotEmpty()
            .WithMessage("LessonId is required");
    }
}