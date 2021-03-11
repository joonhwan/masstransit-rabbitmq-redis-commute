using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TrashLantis.Contracts;

namespace TrashLantis.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TrashController : ControllerBase
    {
        private readonly ILogger<TrashController> _logger;
        private readonly IPublishEndpoint _publishEndpoint;

        public TrashController(ILogger<TrashController> logger, IPublishEndpoint publishEndpoint)
        {
            _logger = logger;
            _publishEndpoint = publishEndpoint;
        }

        [HttpPost("takeout")]
        public async Task<IActionResult> TakeOut(string binNumber)
        {
            _logger.LogInformation("쓰레기 버려라 : {BinNumber}", binNumber);

            await _publishEndpoint.Publish<TakeOutTheTrash>(new
            {
                BinNumber = binNumber,
            });

            return Accepted();
        }
    }
}