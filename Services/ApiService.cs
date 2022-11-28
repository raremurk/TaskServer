using Server.Interfaces;
using Server.Models;

namespace Server.Services
{
    public class ApiService : IApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public Task<List<Rate>> GetCurrencyHistory(int id, DateTime start, DateTime end)
        {
            return id == 4 ? GetBtcHistory(start, end) : GetCurrencyHistoryFromNbrb(id, start, end);
        }

        private async Task<List<Rate>> GetBtcHistory(DateTime start, DateTime end)
        {
            List<Rate> rates = new();
            var client = _httpClientFactory.CreateClient();
            var startUnix = ((DateTimeOffset)start).ToUnixTimeMilliseconds();
            var endUnix = ((DateTimeOffset)end).ToUnixTimeMilliseconds();
            using var response = await client.GetAsync($"https://api.coincap.io/v2/assets/bitcoin/history?interval=d1&start={startUnix}&end={endUnix}");
            var btcData = await response.Content.ReadFromJsonAsync<BtcData>();
            foreach (var rate in btcData.Data)
            {
                rates.Add(new Rate() { Currency = "BTC", Date = rate.Date, Value = (double)Math.Round(rate.PriceUsd, 4), ValueCurrency = "USD", Amount = 1 });
            }
            return rates;
        }

        private async Task<List<Rate>> GetCurrencyHistoryFromNbrb(int id, DateTime start, DateTime end)
        {
            var client = _httpClientFactory.CreateClient();
            var currency = id switch { 1 => "USD", 2 => "EUR", 3 => "RUB", _ => string.Empty };
            var amount = id switch { 1 => 1, 2 => 1, 3 => 100, _ => 0 };
            var ranges = GetRanges(id, start, end);
            var rates = new List<Rate>();

            foreach (var range in ranges)
            {
                using var response = await client.GetAsync($"https://www.nbrb.by/api/exrates/rates/dynamics/{range.Item1}?startDate={range.Item2}&endDate={range.Item3}");
                var data = await response.Content.ReadFromJsonAsync<RateFromNbrb[]>();
                foreach (var rate in data)
                {
                    rates.Add(new Rate() { Currency = currency, Date = rate.Date, Value = (double)rate.Cur_OfficialRate, ValueCurrency = "BYN", Amount = amount });
                }
            }
            return rates;
        }

        private static List<(int, string, string)> GetRanges(int id, DateTime start, DateTime end)
        {
            var curIdChangingDate = new DateTime(2021, 7, 8);
            int curIdBefore = id switch { 1 => 145, 2 => 292, 3 => 298, _ => 0 };
            int curIdAfter = id switch { 1 => 431, 2 => 451, 3 => 456, _ => 0 };

            if (DateTime.Compare(end, curIdChangingDate) <= 0)
            {
                return SplitRange(curIdBefore, start, end);
            }
            else if (DateTime.Compare(start, curIdChangingDate) > 0)
            {
                return SplitRange(curIdAfter, start, end);
            }
            else
            {
                List<(int, string, string)> ranges = new();
                ranges.AddRange(SplitRange(curIdBefore, start, curIdChangingDate));
                ranges.AddRange(SplitRange(curIdAfter, curIdChangingDate.AddDays(1), end));
                return ranges;
            }
        }

        private static List<(int, string, string)> SplitRange(int curId, DateTime start, DateTime end)
        {
            List<(int, string, string)> ranges = new();
            while (end.Subtract(start).Days > 365)
            {
                ranges.Add(new(curId, $"{start:yyyy-MM-dd}", $"{start.AddDays(364):yyyy-MM-dd}"));
                start = start.AddDays(365);
            }
            ranges.Add(new(curId, $"{start:yyyy-MM-dd}", $"{end:yyyy-MM-dd}"));

            return ranges;
        }
    }
}
