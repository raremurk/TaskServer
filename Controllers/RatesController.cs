using Microsoft.AspNetCore.Mvc;
using Server.Models;
using System;

namespace Server.Controllers
{
    [Route("api/GetData")]
    [ApiController]
    public class RatesController : ControllerBase
    {
        private readonly ILogger<RatesController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public RatesController(ILogger<RatesController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RateDTO>>> GetAsync(int id, DateTime start, DateTime end)
        {
            if (id < 1 || id > 4
                || DateTime.Compare(start, end) > 0
                || DateTime.Compare(start, DateTime.Today) > 0
                || DateTime.Compare(end, DateTime.Today) > 0
                || DateTime.Compare(start, new DateTime(2016,1,1)) < 0)
            {
                return BadRequest();
            }

            List<RateDTO> rates = new();
            var client = _httpClientFactory.CreateClient();

            if (id == 4)
            {
                var startUnix = ((DateTimeOffset)start).ToUnixTimeMilliseconds();
                var endUnix = ((DateTimeOffset)end).ToUnixTimeMilliseconds();
                using var response = await client.GetAsync($"https://api.coincap.io/v2/assets/bitcoin/history?interval=d1&start={startUnix}&end={endUnix}");
                var btcData = await response.Content.ReadFromJsonAsync<BtcData>();
                foreach (var rate in btcData.Data)
                {
                    rates.Add(new RateDTO() { CurrencyId = id, PriceCurrency = "USD", Price = (double)Math.Round(rate.PriceUsd, 4), Date = rate.Date });
                }
            }
            else
            {
                List<(int, string, string)> ranges = GetRanges(id, start, end);
                foreach (var range in ranges)
                {
                    using var response = await client.GetAsync($"https://www.nbrb.by/api/exrates/rates/dynamics/{range.Item1}?startDate={range.Item2}&endDate={range.Item3}");
                    var data = await response.Content.ReadFromJsonAsync<Rate[]>();
                    foreach (var rate in data)
                    {
                        rates.Add(new RateDTO() { CurrencyId = id, PriceCurrency = "BYN", Price = (double)rate.Cur_OfficialRate, Date = rate.Date });
                    }
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
                ranges.Add(new(curId, $"{start: yyyy-MM-dd}", $"{start.AddDays(364): yyyy-MM-dd}"));
                start = start.AddDays(365);
            }
            ranges.Add(new(curId, $"{start: yyyy-MM-dd}", $"{end: yyyy-MM-dd}"));

            return ranges;
        }
    }
}
