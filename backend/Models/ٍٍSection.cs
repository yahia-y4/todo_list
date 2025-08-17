namespace ToDoApp.Models
{
    public class Section
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public DateTime createDate { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public List<ToDoTask> ToDoTasks { get; set; } = new();

    }
}