using AITrainingSystem.Application.DTOs.Course;
using FluentValidation;

namespace AITrainingSystem.Application.Validators.Course;

public class CreateCourseValidator : AbstractValidator<CreateCourseDto>
{
    public CreateCourseValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Course title is required.")
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required.")
            .MinimumLength(20)
            .MaximumLength(2000);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Price cannot be negative.");

        RuleFor(x => x.DurationInHours)
            .GreaterThan(0)
            .WithMessage("Duration must be greater than 0.");

        RuleFor(x => x.ThumbnailUrl)
            .Must(url =>
                string.IsNullOrWhiteSpace(url) ||
                Uri.IsWellFormedUriString(url, UriKind.Absolute))
            .WithMessage("Thumbnail URL is invalid.");
    }
}