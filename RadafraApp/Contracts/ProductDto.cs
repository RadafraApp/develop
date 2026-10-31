namespace RadafraApp.Contracts
{
    public class ProductDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string ImagePath { get; set; } = string.Empty;

        public string ThumbnailPath { get; set; } = string.Empty;

        public decimal StartingPrice { get; set; }

        public decimal CurrentPrice { get; set; }

        public int BidCount { get; set; }

        public string? WinnerPhone { get; set; }

        public string Publisher { get; set; } = string.Empty;

        public string Collection { get; set; } = string.Empty;

        public string Equipo { get; set; } = string.Empty;

        public string TipoCromo { get; set; } = string.Empty;

        public bool IsNumbered { get; set; }

        public string? Numbering { get; set; }

        public bool Pagado { get; set; }

        public bool Enviado { get; set; }

        public DateTimeOffset AuctionEndTime { get; set; }
    }
}
