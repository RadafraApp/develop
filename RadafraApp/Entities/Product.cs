using System.ComponentModel.DataAnnotations;

namespace RadafraApp.Entities
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        // Legacy file path; retained only to map pre-existing uploads during backfill.
        [MaxLength(500)]
        public string? ImagePath { get; set; }

        // Image bytes are stored in the database so they survive container rebuilds.
        public byte[]? ImageData { get; set; }

        [MaxLength(100)]
        public string? ImageContentType { get; set; }

        public DateTimeOffset? ImageUpdatedAt { get; set; }

        // Small rendition (max 800x600) used by the auction cards and tables.
        public byte[]? ThumbnailData { get; set; }

        [MaxLength(100)]
        public string? ThumbnailContentType { get; set; }

        [Range(0, 1_000_000)]
        public decimal StartingPrice { get; set; }

        [Required]
        [MaxLength(200)]
        public string Publisher { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Collection { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Equipo { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string TipoCromo { get; set; } = string.Empty;

        public bool IsNumbered { get; set; }

        [MaxLength(100)]
        public string? Numbering { get; set; }

        public bool Pagado { get; set; }

        public bool Enviado { get; set; }

        public DateTimeOffset AuctionEndTime { get; set; }

        public ICollection<Bid> Bids { get; set; } = new List<Bid>();
    }
}
