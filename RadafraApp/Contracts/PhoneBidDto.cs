namespace RadafraApp.Contracts
{
    public class PhoneBidDto
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string ImagePath { get; set; } = string.Empty;

        public decimal Amount { get; set; }
    }
}
