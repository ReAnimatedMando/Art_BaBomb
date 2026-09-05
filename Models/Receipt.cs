using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Art_BaBomb.Web.Models
{
    public class Receipt
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        public Project? Project { get; set; }

        [Required]
        [StringLength(200)]
        public string Vendor { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime PurchaseDate { get; set; } = DateTime.Today;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Range(0.01, 999999.99)]
        public decimal TotalAmount { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        public string? ReceiptFileName { get; set; }

        public string? ReceiptPath { get; set; }

        public long? ReceiptSizeBytes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}