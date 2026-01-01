using AngularApp4.Model;
using Microsoft.AspNetCore.Mvc;

namespace AngularApp4.Controllers
{
    [ApiController]
    [Route("api/todo")] // This makes the URL: /api/todo
    public class ToDoListController : ControllerBase
    {
        // Mock Database
        private static readonly List<TodoItem> Todos = new List<TodoItem>
        {
            new TodoItem { Id = 1, Task = "Fix the Angular CLI", IsCompleted = true },
            new TodoItem { Id = 2, Task = "Fetch Data from API", IsCompleted = false },
            new TodoItem { Id = 3, Task = "Celebrate!", IsCompleted = false }
        };

        [HttpGet]
        public IEnumerable<TodoItem> Get()
        {
            return Todos;
        }

        [HttpPost]
        public IActionResult Post(TodoItem item)
        {
            item.Id = Todos.Count + 1;
            Todos.Add(item);
            return Ok(item);
        }
    }
}
