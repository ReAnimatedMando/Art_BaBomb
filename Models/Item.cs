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

        [Range(1, 9999)]
        public int Quantity { get; set; } = 1;

        [StringLength(100)]
        public string? Scene { get; set; }

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

        public string? PurchaseReceiptFileName { get; set; }

        public string? PurchaseReceiptPath { get; set; }

        public bool IsReturnRequired { get; set; } = false;

        public string? ReturnNotes { get; set; }

        public string? ReturnLocation { get; set; }

        public DateTime? ReturnByDate { get; set; }

        public bool IsReturned { get; set; } = false;

        public DateTime? ReturnedAt { get; set; }

        public string? ReturnReceiptFileName { get; set; }

        public string? ReturnReceiptPath { get; set; }

        public long? PurchaseReceiptSizeBytes { get; set; }

        public long? ReturnReceiptSizeBytes { get; set; }

        [NotMapped]
        public bool IsNeeded =>
            string.IsNullOrWhiteSpace(Status) ||
            Status.Trim().Equals("Needed", StringComparison.OrdinalIgnoreCase);

        [NotMapped]
        public bool IsAcquired =>
            !string.IsNullOrWhiteSpace(Status) &&
            Status.Trim().Equals("Acquired", StringComparison.OrdinalIgnoreCase);

        [NotMapped]
        public bool IsInReturnQueue =>
            IsReturnRequired && !IsReturned;

        [NotMapped]
        public bool NeedsPurchaseReceipt =>
            ActualCost.HasValue &&
            ActualCost.Value > 0 &&
            string.IsNullOrWhiteSpace(PurchaseReceiptPath);

        [NotMapped]
        public bool MissingReturnByDate =>
            IsInReturnQueue && !ReturnByDate.HasValue;

        [NotMapped]
        public bool HasPastReturnByDate =>
            IsInReturnQueue &&
            ReturnByDate.HasValue &&
            ReturnByDate.Value.Date < DateTime.Today;

        [NotMapped]
        public bool NeedsReturnReceipt =>
            IsReturned && string.IsNullOrWhiteSpace(ReturnReceiptPath);

        [NotMapped]
        public bool NeedsAttention =>
            NeedsPurchaseReceipt ||
            MissingReturnByDate ||
            HasPastReturnByDate ||
            NeedsReturnReceipt;

        [NotMapped]
        public string WorkflowState
        {
            get
            {
                if (IsReturned)
                {
                    return "Returned";
                }

                if (IsInReturnQueue)
                {
                    return "ReturnQueue";
                }

                if (IsAcquired)
                {
                    return "Acquired";
                }

                return "Needed";
            }
        }

        [NotMapped]
        public string? AttentionMessage
        {
            get
            {
                if (HasPastReturnByDate)
                {
                    return "Return date is overdue.";
                }

                if (MissingReturnByDate)
                {
                    return "Missing return-by date.";
                }

                if (NeedsReturnReceipt)
                {
                    return "Missing return receipt.";
                }

                if (NeedsPurchaseReceipt)
                {
                    return "Missing purchase receipt.";
                }

                return null;
            }
        }
    }
}