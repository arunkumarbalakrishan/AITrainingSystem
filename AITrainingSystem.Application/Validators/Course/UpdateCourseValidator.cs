using AITrainingSystem.Application.DTOs.Course;
using FluentValidation;

namespace AITrainingSystem.Application.Validators.Course;
    
public class UpdateCourseValidator : AbstractValidator<UpdateCourseDto>
{
    public UpdateCourseValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MinimumLength(20)
            .MaximumLength(2000);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.DurationInHours)
            .GreaterThan(0);

        RuleFor(x => x.ThumbnailUrl)
            .Must(url =>
                string.IsNullOrWhiteSpace(url) ||
                Uri.IsWellFormedUriString(url, UriKind.Absolute))
            .WithMessage("Thumbnail URL is invalid.");
    }
}