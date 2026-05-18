using AITrainingSystem.Application.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace AITrainingSystem.Application.DTOs.Users
{
    public class UserQueryParams : PaginationParams
    {
        public string? Search { get; set; }

        public string? Role { get; set; }
    }
}
