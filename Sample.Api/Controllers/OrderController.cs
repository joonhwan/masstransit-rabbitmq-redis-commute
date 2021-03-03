using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sample.Contracts;

namespace Sample.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderController : ControllerBase
    {
        private ILogger<OrderController> _logger;
        private IRequestClient<SubmitOrder> _submitOrderRequestClient;
        private readonly IRequestClient<CheckOrder> _checkOrderRequestClient;
        private readonly ISendEndpointProvider _sendEndpointProvider;
        private readonly IPublishEndpoint _publishEndpoint;

        public OrderController(
            ILogger<OrderController> logger,
            IRequestClient<SubmitOrder> submitOrderRequestClient,
            IRequestClient<CheckOrder> checkOrderRequestClient,
            ISendEndpointProvider sendEndpointProvider,
            IPublishEndpoint publishEndpoint
        )
        {
            _logger = logger;
            _submitOrderRequestClient = submitOrderRequestClient;
            _checkOrderRequestClient = checkOrderRequestClient;
            _sendEndpointProvider = sendEndpointProvider;
            _publishEndpoint = publishEndpoint;
        }

        [HttpGet]
        public async Task<IActionResult> Get(Guid id)
        {
            try
            {
                // var response = await _checkOrderRequestClient.GetResponse<OrderStatus>(new
                // {
                //     OrderId = orderId
                // });
                //
                // return Ok(response.Message);
                var (status, notFound) = await _checkOrderRequestClient.GetResponse<OrderStatus, OrderNotFound>(new
                {
                    OrderId = id
                });
                if (status.IsCompletedSuccessfully)
                {
                    var response = await status;
                    return Ok(response.Message);
                }
                else
                {
                    var response = await notFound;
                    return NotFound(new
                    {
                        Message = "Order Not Found!",
                        MissingOrderId = response.Message.OrderId
                    });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
        }
        
        [HttpPost]
        public async Task<IActionResult> Post(Guid id, string customerNumber)
        {
            // Consumer 는 1개 이상의 Message를 반환할 수 있다.
            // @more-than-one-response-message 
            var (accepted, rejected) = await _submitOrderRequestClient.GetResponse<OrderSubmissionAccepted, OrderSubmissionRejected>(new
            {
                OrderId = id,
                Timestamp = InVar.Timestamp,//TimeStamp = InVar.Timestamp,
                CustomerNumber = customerNumber
            });

            if (accepted.IsCompletedSuccessfully)
            {
                var response = await accepted;
                return Accepted(response.Message);
            }
            else
            {
                var response = await rejected;
                return BadRequest(response.Message);   
            }
        }

        [HttpPatch]
        public async Task<IActionResult> Patch(Guid id)
        {
            Console.WriteLine("@@@ OrderAccepted 시뮬레이션..");
            await _publishEndpoint.Publish<OrderAccepted>(new
            {
                OrderId = id,
                Timestamp = InVar.Timestamp,
            });

            return Accepted();
        }
        
        [HttpPut]
        public async Task<IActionResult> Put(Guid id, string customerNumber)
        {
            var endpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri("exchange:submit-order")); // was "exchange: ...."
            await endpoint.Send<SubmitOrder>(new
            {
                OrderId = id,
                Timestamp = InVar.Timestamp,//TimeStamp = InVar.Timestamp,
                CustomerNumber = customerNumber
            });

            return Accepted();
        }
    }
    
    
}