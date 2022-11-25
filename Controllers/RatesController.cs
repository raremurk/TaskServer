using Microsoft.AspNetCore.Mvc;
using Server.Models;
using System;
using System.Formats.Tar;
using System.Text.Json;

namespace Server.Controllers
{
    [Route("api/GetData")]
    [ApiController]
    public class RatesController : ControllerBase
    {
        private List<Rate> rates = new();
        private readonly ILogger<RatesController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public RatesController(ILogger<RatesController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Rate>>> GetAsync(int id, DateTime start, DateTime end)
        {
            if (id < 1 || id > 4
                || DateTime.Compare(start, end) > 0
                || DateTime.Compare(start, DateTime.Today) > 0
                || DateTime.Compare(end, DateTime.Today) > 0
                || DateTime.Compare(start, new DateTime(2017, 1, 1)) < 0)
            {
                return BadRequest();
            }

            var client = _httpClientFactory.CreateClient();
            string currency = id switch { 1 => "USD", 2 => "EUR", 3 => "RUB", 4 => "BTC", _ => string.Empty };
            string valueCurrency = id switch { 1 => "BYN", 2 => "BYN", 3 => "BYN", 4 => "USD", _ => string.Empty };
            int amount = id switch { 1 => 1, 2 => 1, 3 => 100, 4 => 1, _ => 0 };

            if (id == 4)
            {
                var startUnix = ((DateTimeOffset)start).ToUnixTimeMilliseconds();
                var endUnix = ((DateTimeOffset)end).ToUnixTimeMilliseconds();
                using var response = await client.GetAsync($"https://api.coincap.io/v2/assets/bitcoin/history?interval=d1&start={startUnix}&end={endUnix}");
                var btcData = await response.Content.ReadFromJsonAsync<BtcData>();
                foreach (var rate in btcData.Data)
                {
                    rates.Add(new Rate() { Currency = currency, Date = rate.Date, Value = (double)Math.Round(rate.PriceUsd, 4), ValueCurrency = valueCurrency, Amount = amount });
                }
            }
            else
            {
                List<(int, string, string)> ranges = GetRanges(id, start, end);
                foreach (var range in ranges)
                {
                    using var response = await client.GetAsync($"https://www.nbrb.by/api/exrates/rates/dynamics/{range.Item1}?startDate={range.Item2}&endDate={range.Item3}");
                    var data = await response.Content.ReadFromJsonAsync<RateFromNbrb[]>();
                    foreach (var rate in data)
                    {
                        rates.Add(new Rate() { Currency = currency, Date = rate.Date, Value = (double)rate.Cur_OfficialRate, ValueCurrency = valueCurrency, Amount = amount });
                    }
                }
            }

            WriteToJson();
            return rates;
        }

        private void ReadFromJson()
        {
            using (FileStream fstream = new FileStream("data.json", FileMode.OpenOrCreate)) ;

            using (StreamReader r = new StreamReader("data.json"))
            {
                string json = r.ReadToEnd();
                if (!string.IsNullOrEmpty(json))
                {
                    rates = JsonSerializer.Deserialize<List<Rate>>(json);
                }
            }
        }

        private void WriteToJson()
        {
            var options = new JsonSerializerOptions() { WriteIndented = true };
            options.Converters.Add(new CustomDateTimeConverter("dd/MM/yy"));
            string jsonString = JsonSerializer.Serialize(rates, options);
            using (StreamWriter outputFile = new StreamWriter("data.json", append: false))
            {
                outputFile.WriteLine(jsonString);
            }
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
