namespace RadafraApp.Contracts
{
    public class BidDto
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public string Phone { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
