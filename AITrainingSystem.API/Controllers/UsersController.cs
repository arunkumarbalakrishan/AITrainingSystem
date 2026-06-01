using AITrainingSystem.Application.Common.Models;
using AITrainingSystem.Application.DTOs.Common;
using AITrainingSystem.Application.DTOs.User;
using AITrainingSystem.Application.DTOs.Users;
using AITrainingSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AITrainingSystem.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim == null)
            return Unauthorized();

        var userId = Guid.Parse(userIdClaim.Value);

        var user = await _userService.GetCurrentUserAsync(userId);

        if (user == null)
            return NotFound();

        return Ok(ApiResponse<UserResponseDto>.SuccessResponse(user, "User retrieved successfully"));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAllUsers(
     [FromQuery] UserQueryParams queryParams)
    {
        var result = await _userService.GetAllUsersAsync(queryParams);

        return Ok(ApiResponse<PagedResult<UserResponseDto>>.SuccessResponse(result, "Users retrieved successfully"));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var user = await _userService.GetUserByIdAsync(id);

        if (user == null)
            return NotFound();

        return Ok(ApiResponse<UserResponseDto>.SuccessResponse(user, "User retrieved successfully"));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUser(Guid id, UpdateUserDto dto)
    {
        var updated = await _userService.UpdateUserAsync(id, dto);

        if (!updated)
            return NotFound();

        return Ok(new
        {
            Message = "User updated successfully"
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var deleted = await _userService.DeleteUserAsync(id);

        if (!deleted)
            return NotFound();

        return Ok(new
        {
            Message = "User deleted successfully"
        });
    }

}