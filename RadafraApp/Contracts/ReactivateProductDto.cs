using System.ComponentModel.DataAnnotations;

namespace RadafraApp.Contracts
{
    public class ReactivateProductDto
    {
        [Range(0, 1_000_000)]
        public decimal StartingPrice { get; set; }

        [Required]
        public DateTimeOffset AuctionEndTime { get; set; }
    }
}
