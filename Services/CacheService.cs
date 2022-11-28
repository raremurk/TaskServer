using Server.Interfaces;
using Server.Models;

namespace Server.Services
{
    public class CacheService : ICacheService
    {
        private readonly IWorkingWithJsonService _workingWithJsonService;
        private readonly IApiService _apiService;
        private List<Rate> rates = new();

        public CacheService(IWorkingWithJsonService workingWithJsonService, IApiService apiService)
        {
            _workingWithJsonService = workingWithJsonService;
            _apiService = apiService;
            UpdateData(_workingWithJsonService.ReadFromJson());
        }

        public List<Rate> GetData(int id, DateTime start, DateTime end)
        {
            string currency = id switch { 1 => "USD", 2 => "EUR", 3 => "RUB", 4 => "BTC", _ => string.Empty };
            var dataFromCache = DataExists(currency, start, end);
            if (!dataFromCache.Item1)
            {
                var dataFromApi = _apiService.GetCurrencyHistory(id, start, end).Result;
                UpdateData(dataFromApi);
            }

            return rates.Where(x =>
                string.Compare(x.Currency, currency) == 0
                && DateTime.Compare(x.Date, start) >= 0
                && DateTime.Compare(x.Date, end) <= 0).ToList();
        }

        private (bool, DateTime, DateTime) DataExists(string currency, DateTime start, DateTime end)
        {
            var missingRates = new List<DateTime>();
            while (DateTime.Compare(start, end) <= 0)
            {
                var rateExists = RateExists(currency, start);
                if (!rateExists)
                {
                    missingRates.Add(start);
                }
                start = start.AddDays(1);
            }
            var exists = missingRates.Count == 0;
            start = exists ? DateTime.MinValue : missingRates.Min();
            end = exists ? DateTime.MinValue : missingRates.Max();

            return (exists, start, end);
        }

        private void UpdateData(List<Rate> additionalRates)
        {
            rates.AddRange(additionalRates.Where(rate => !RateExists(rate.Currency, rate.Date)));
            rates = rates.OrderBy(x => x.Date).ToList();
            _workingWithJsonService.WriteToJson(rates);
        }

        private bool RateExists(string currency, DateTime date)
        {
            var rate = rates.Find(x => string.Compare(x.Currency, currency) == 0 && DateTime.Compare(x.Date, date) == 0);
            return rate is not null;
        }
    }
}
