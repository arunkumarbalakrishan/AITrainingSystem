using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AITrainingSystem.Application.DTOs.User;

public class UpdateUserDto
{
    public string FullName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool? IsApprovedTrainer { get; set; }
}