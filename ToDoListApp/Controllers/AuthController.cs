using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ToDoListApp.Data;
using ToDoListApp.Dtos;
using ToDoListApp.Models;

namespace ToDoListApp.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AppDBContext _db;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthController(AppDBContext db)
    {
        _db = db;
    }

    /* POST /auth/register
     * Retrieve email and password from request body
     * Validate email and password
     * Hash password
     * Create new user
     * Save user to database
     * Return success response
     */

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {

        var email = (request.Email ?? string.Empty).Trim().ToLower();
        var password = request.Password ?? string.Empty;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            return BadRequest("Email and password are required.");
        }

        if (password.Length < 8)
        {
            return BadRequest("Password must be at least 6 characters long.");
        }

        var existingUser = await _db.Users.AnyAsync(u => u.Email == email);
        if (existingUser)
        {
            return Conflict(new { error = "a user already exists with this email."});
        }

        var user = new User { Email = email };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        _db.Add(user);
        await _db.SaveChangesAsync();

        return Created(
            $"/users/{user.Id}",
            new { id = user.Id, email = user.Email , user.CreatedAtUtc }
            );
    }

    [HttpPost("login")]
    public IActionResult Login(LoginRequest request)
    {
        return Ok("Login successful");
    }
}
