using System.ComponentModel.DataAnnotations;

namespace AITrainingSystem.Application.DTOs.User;

public class UpdateProfileDto
{
    [Required]
    public string FullName { get; set; } = string.Empty;

    public string? CurrentPassword { get; set; }

    public string? NewPassword { get; set; }
}
