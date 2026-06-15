using AITrainingSystem.Application.DTOs.Lesson;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AITrainingSystem.Application.Validators.Lesson
{
    public class CreateLessonValidator : AbstractValidator<CreateLessonDto>
    {
        public CreateLessonValidator()
        {
            RuleFor(x => x.CourseId)
                .NotEmpty();

            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.DurationInMinutes)
                .GreaterThan(0);

           
        }
    }
}
