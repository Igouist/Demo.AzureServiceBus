using Microsoft.AspNetCore.Mvc;

namespace ServiceBusDemo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QueueController : ControllerBase
    {
        public QueueController()
        {
        }

        [HttpGet]
        public IEnumerable<string> Get()
        {
            return Enumerable.Empty<string>();
        }
    }
}