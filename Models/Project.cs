using System.ComponentModel.DataAnnotations;

namespace Art_BaBomb.Web.Models
{
    public class Project
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public ProjectDepartment Department { get; set; } = ProjectDepartment.SetDec;

        [StringLength(1000)]
        public string? Description { get; set; }

        public decimal Budget { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Item> Items { get; set; } = new List<Item>();
    }
}