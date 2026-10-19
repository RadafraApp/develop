using System.ComponentModel.DataAnnotations;

namespace RadafraApp.Contracts
{
    public class CreateProductDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(0, 1_000_000)]
        public decimal StartingPrice { get; set; }

        [Required]
        [MaxLength(200)]
        public string Publisher { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Collection { get; set; } = string.Empty;

        public bool IsNumbered { get; set; }

        [MaxLength(100)]
        public string? Numbering { get; set; }

        [Required]
        public DateTimeOffset AuctionEndTime { get; set; }

        [Required]
        public IFormFile? Image { get; set; }
    }
}
