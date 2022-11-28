using Server.Models;

namespace Server.Interfaces
{
    public interface IApiService
    {
        public Task<List<Rate>> GetCurrencyHistory(int id, DateTime start, DateTime end);
    }
}
