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

        [HttpPost("/Create")]
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

        [HttpGet("/GetAllTasks")]
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

        [HttpGet("/specificTask/{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userId = GetUserId();
            var task = await _db.Tasks
                .Where(t => t.UserId == userId && t.Id == id)
                .Select(t => new
                {
                    t.Id,
                    t.Title,
                    t.Description,
                    t.IsCompleted,
                    t.CreatedAtUtc,
                    t.CompletedAtUtc
                })
                .FirstOrDefaultAsync();
            if (task == null)
            {
                return NotFound(new { error = "Task not found." });
            }
            return Ok(task);
        }

        [HttpPut("/update/{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskRequest request)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized(new { error = "Invalid token." });
            }
            var task = await _db.Tasks.FirstOrDefaultAsync(t => t.UserId == userId && t.Id == id);
            if (task == null)
            {
                return NotFound(new { error = "Task not found." });
            }
            var title = (request.Title ?? string.Empty).Trim();
            var description = request.Description?.Trim();
            if (string.IsNullOrEmpty(title))
            {
                return BadRequest(new { error = "Title is required." });
            }
            task.Title = title;
            task.Description = description;
            task.IsCompleted = request.IsCompleted ?? task.IsCompleted;

            if (task.IsCompleted)
            {
                task.CompletedAtUtc = DateTime.UtcNow;
            }
            else
            {
                task.CompletedAtUtc = null;
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                task.Id,
                task.Title,
                task.Description,
                task.IsCompleted,
                task.CreatedAtUtc,
                task.CompletedAtUtc
            });
        }

        [HttpDelete("/delete/{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetUserId();
            var task = await _db.Tasks.FirstOrDefaultAsync(t => t.UserId == userId && t.Id == id);
            if (task == null)
            {
                return NotFound(new { error = "Task not found." });
            }
            _db.Tasks.Remove(task);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("/markComplete/{id:guid}")]
        public async Task<IActionResult> MarkAsCompleted(Guid id)
        {
            var userId = GetUserId();
            var task = await _db.Tasks.FirstOrDefaultAsync(t => t.UserId == userId && t.Id == id);
            if (task == null)
            {
                return NotFound(new { error = "Task not found." });
            }
            task.IsCompleted = true;
            task.CompletedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new
            {
                task.Id,
                task.Title,
                task.Description,
                task.IsCompleted,
                task.CreatedAtUtc,
                task.CompletedAtUtc
            });
        }
    }
}
