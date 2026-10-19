using System.ComponentModel.DataAnnotations;

namespace RadafraApp.Contracts
{
    public class CreateBidDto
    {
        [Required]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 1_000_000)]
        public decimal Amount { get; set; }
    }
}
