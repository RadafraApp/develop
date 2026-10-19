using System.ComponentModel.DataAnnotations;

namespace RadafraApp.Entities
{
    public class Bid
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public Product? Product { get; set; }

        [Required]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Range(0.01, 1_000_000)]
        public decimal Amount { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
