using Microsoft.AspNetCore.Mvc;

namespace ServiceBusDemo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QueueController : ControllerBase
    {
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return Enumerable.Empty<string>();
        }
    }
}