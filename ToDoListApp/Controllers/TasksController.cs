using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ToDoListApp.Data;
using ToDoListApp.Dtos;
using ToDoListApp.Models;

namespace ToDoListApp.Controllers
{


    [Authorize]
    [ApiController]
    [Route("tasks")]
    public class TasksController : ControllerBase
    {
        private readonly AppDBContext _db;

        public TasksController(AppDBContext db)
        {
            _db = db;
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid token.");
            }

            return userId;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTaskRequest request)
        {
            var userId = GetUserId();

            var title = (request.Title ?? string.Empty).Trim();
            var description = request.Description?.Trim();

            if (string.IsNullOrEmpty(title))
            {
                return BadRequest(new { error = "Title is required." });
            }

            var task = new ToDoTask
            {
                UserId = userId,
                Title = title,
                Description = description,
            };

            _db.Tasks.Add(task);
            await _db.SaveChangesAsync();

            return Created($"/tasks/{task.Id}", new
            {
                task.Id,
                task.Title,
                task.Description,
                task.IsCompleted,
                task.CreatedAtUtc
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetUserId();

            var tasks = await _db.Tasks
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAtUtc)
                .Select(t => new
                {
                    t.Id,
                    t.Title,
                    t.Description,
                    t.IsCompleted,
                    t.CreatedAtUtc,
                    t.CompletedAtUtc
                })
                .ToListAsync();

            return Ok(tasks);
        }
    }
}
