using Server.Models;

namespace Server.Interfaces
{
    public interface IWorkingWithJsonService
    {
        public List<Rate> ReadFromJson();

        public void WriteToJson(List<Rate> rates);
    }
}
