using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ToDoListApp.Data;
using ToDoListApp.Dtos;
using ToDoListApp.Models;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ToDoListApp.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AppDBContext _db;
    private readonly PasswordHasher<User> _passwordHasher = new();
    private readonly IConfiguration _config;

    public AuthController(AppDBContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
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
            return BadRequest("Password must be at least 8 characters long.");
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
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLower();
        var password = request.Password ?? string.Empty;
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            return BadRequest("Email and password are required.");
        }
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            return Unauthorized(new { error = "Invalid email or password." });
        }
        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { error = "Invalid email or password." });
        }
        var token = CreateJWT(user);
        return Ok(new { token });
    }

    private string CreateJWT(User user)
    {
        var jwtSection = _config.GetSection("JWT");

        var key = jwtSection["Key"];
        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];
        var expiresInMinutes = int.Parse(jwtSection["DurationInMinutes"]);

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: creds
            );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}
