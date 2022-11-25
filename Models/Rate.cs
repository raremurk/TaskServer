namespace Server.Models
{
    public class Rate
    {
        public string Currency { get; set; }
        public DateTime Date { get; set; }
        public double Value { get; set; }
        public string ValueCurrency { get; set; }
        public int Amount { get; set; }
    }
}
