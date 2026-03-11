namespace ToDoListApp.Dtos
{
    public record UpdateTaskRequest
    (
        string? Title,
        string? Description,
        bool? IsCompleted
    );
}
