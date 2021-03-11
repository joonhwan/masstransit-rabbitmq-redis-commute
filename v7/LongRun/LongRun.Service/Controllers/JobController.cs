using System;
using System.Threading.Tasks;
using LongRun.Contracts;
using MassTransit;
using MassTransit.Conductor.Client;
using MassTransit.Contracts.JobService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LongRun.Service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class JobController : ControllerBase
    {
        private readonly ILogger<JobController> _logger;
        private readonly IRequestClient<DoIt> _client;

        public JobController(ILogger<JobController> logger, IRequestClient<DoIt> client)
        {
            _logger = logger;
            _client = client;
        }
        [HttpPost]
        public async Task<IActionResult> Post(string command, string duration)
        {
            if (!TimeSpan.TryParse(duration, out var durationTime))
            {
                return BadRequest();
            }
            
            _logger.LogInformation("신규 작업 : 소요시간 = {Duration}", duration);

            // await _publishEndpoint.Publish<DoIt>(new
            // {
            //     Duration = durationTime
            // });
            var response = await _client.GetResponse<JobSubmissionAccepted>(new
            {
                Duration = durationTime,
                Command = command ?? "normal"
            });
            _logger.LogInformation("Job 제출 완료 : {Response}", response);
            
            return Ok(new
            {
                JobId = response.Message.JobId,
                Duration = durationTime,
            });
        }
    }
}