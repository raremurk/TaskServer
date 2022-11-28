using Server.Models;

namespace Server.Interfaces
{
    public interface ICacheService
    {
        public List<Rate> GetData(int id, DateTime start, DateTime end);
    }
}
