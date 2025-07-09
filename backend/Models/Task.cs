using System.ComponentModel.DataAnnotations;

namespace ToDoApp.Models
{
    public class ToDoTask
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "المحتوى مطلوب")]
        [MinLength(1, ErrorMessage = "لا يمكن ترك المحتوى فارغًا")]
        public string Content { get; set; } = "";

        [Required(ErrorMessage = "الاسم مطلوب")]
        [MinLength(1, ErrorMessage = "لا يمكن ترك الاسم فارغًا")]
        public string Name { get; set; } = "";

        public bool completed { get; set; } = false;

        public DateTime? EndTask { get; set; } = null;

        public DateTime CreateDate { get; set; } = DateTime.Now;

        public int SectionId { get; set; }

        public Section Section { get; set; } = null!;
    }
}
