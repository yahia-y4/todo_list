using Microsoft.EntityFrameworkCore;
using ToDoApp.Models;

namespace ToDoApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }


        public DbSet<User> Users { get; set; } =null!;
        public DbSet<ToDoTask> ToDoTasks { get; set; } =null!;
        public DbSet<Section> Sections{ get; set; }=null!;
    }
}
