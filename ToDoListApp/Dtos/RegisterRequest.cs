namespace ToDoListApp.Dtos;

    public record RegisterRequest(
        string Email,
        string Password
    );