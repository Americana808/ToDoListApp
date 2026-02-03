using Microsoft.AspNetCore.Mvc;
using ToDoListApp.Data;
using ToDoListApp.Dtos;

namespace ToDoListApp.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AppDBContext _db;

    public AuthController(AppDBContext db)
    {
        _db = db;
    }

    [HttpPost("register")]
    public IActionResult Register(RegisterRequest request)
    {
        return Ok("Registration successful");
    }

    [HttpPost("login")]
    public IActionResult Login(LoginRequest request)
    {
        return Ok("Login successful");
    }
}
