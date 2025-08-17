using Microsoft.EntityFrameworkCore;

namespace ToDoApp.Models
{
    [Index(nameof(Username), IsUnique = true)]
    public class User
    {

        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";

        public List<Section> Sections { get; set; } = new();
        public List<ToDoTask> ToDoTasks { get; set; } = new();

    }
}