using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Sample.Contracts;

namespace Sample.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CustomerController : ControllerBase
    {
        private readonly IPublishEndpoint _publishEndpoint;

        public CustomerController(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }
        
        // @more-saga-1 OrderStateMachine 이 전혀 다른 Event 를 받는 것을 시연. --> 특정 사용자가 탈퇴한 경우.
        [HttpDelete]
        public async Task<IActionResult> Delete(Guid id, string customerNumber)
        {
            await _publishEndpoint.Publish<CustomerAccountClosed>(new
            {
                CustomerId = id,
                Timestamp = InVar.Timestamp,
                CustomerNumber = customerNumber
            });

            return Ok();
        }
    }
}