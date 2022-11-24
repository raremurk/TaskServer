namespace Server.Models
{
    public class RateDTO
    {
        public int CurrencyId { get; set; }
        public string PriceCurrency { get; set; }        
        public double Price { get; set; }
        public DateTime Date { get; set; }
    }
}
