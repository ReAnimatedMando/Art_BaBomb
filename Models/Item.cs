using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Art_BaBomb.Web.Models
{
    public class Item
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        public Project? Project { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public int ItemNumber { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? EstimatedCost { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? ActualCost { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Needed";

        [StringLength(500)]
        public string? ImageUrl { get; set; }
    }
}