using Microsoft.AspNetCore.Mvc;
using Server.Interfaces;
using Server.Models;

namespace Server.Controllers
{
    [Route("api/GetData")]
    [ApiController]
    public class RatesController : ControllerBase
    {
        private readonly ILogger<RatesController> _logger;
        private readonly ICacheService _casheService;

        public RatesController(ILogger<RatesController> logger, ICacheService casheService)
        {
            _logger = logger;
            _casheService = casheService;
        }

        [HttpGet]
        public ActionResult<IEnumerable<Rate>> Get(int id, DateTime start, DateTime end)
        {
            if (id < 1 || id > 4
                || DateTime.Compare(start, end) > 0
                || DateTime.Compare(start, DateTime.Today) > 0
                || DateTime.Compare(end, DateTime.Today) > 0
                || DateTime.Compare(start, new DateTime(2017, 1, 1)) < 0)
            {
                return BadRequest();
            }

            return _casheService.GetData(id, start, end);
        }
    }
}
