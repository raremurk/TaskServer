using Server.Helpers;
using Server.Interfaces;
using Server.Models;
using System.Text.Json;

namespace Server.Services
{
    public class WorkingWithJsonService : IWorkingWithJsonService
    {
        private readonly string fileName = "data.json";
        private readonly JsonSerializerOptions options = new() { WriteIndented = true };

        public WorkingWithJsonService()
        {
            options.Converters.Add(new CustomDateTimeConverter("dd/MM/yy"));
        }

        public List<Rate> ReadFromJson()
        {
            using (FileStream fstream = new(fileName, FileMode.OpenOrCreate)) ;
            using var r = new StreamReader(fileName);
            string json = r.ReadToEnd();
            var rates = new List<Rate>();
            if (!string.IsNullOrEmpty(json))
            {
                return JsonSerializer.Deserialize<List<Rate>>(json, options);
            }

            return rates;
        }

        public void WriteToJson(List<Rate> rates)
        {
            string jsonString = JsonSerializer.Serialize(rates, options);
            using var outputFile = new StreamWriter(fileName, append: false);
            outputFile.WriteLine(jsonString);
        }
    }
}
