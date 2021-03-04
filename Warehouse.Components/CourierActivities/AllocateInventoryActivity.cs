using System;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Courier;
using MassTransit.Courier.Results;
using Microsoft.Extensions.Logging;
using Warehouse.Contracts;

namespace Warehouse.Components.CourierActivities
{
    public class AllocateInventoryActivity : MassTransit.Courier.IActivity<AllocateInventoryArguments, AllocateInventoryLog>
    {
        private readonly IRequestClient<AllocateInventory> _client;
        private readonly ILogger<AllocateInventoryActivity> _logger;

        public AllocateInventoryActivity(
            IRequestClient<AllocateInventory> client,
            ILogger<AllocateInventoryActivity> logger
        )
        {
            _client = client;
            _logger = logger;
        }

        public class MyInventoryAllocated : InventoryAllocated
        {
            public Guid AllocationId { get; }
            public string ItemNumber { get; }
            public decimal Quantity { get; }

            public MyInventoryAllocated(Guid allocationId, string itemNumber, decimal quantity)
            {
                AllocationId = allocationId;
                ItemNumber = itemNumber;
                Quantity = quantity;
            }
        }

        public async Task<ExecutionResult> Execute(ExecuteContext<AllocateInventoryArguments> context)
        {
            var args = context.Arguments;
            var itemNumber = args.ItemNumber;

            _logger.LogInformation("Allocate Inventory Activity 시작되었음. arg={Arg}", args);
            // 입력 인자 확인 
            if (string.IsNullOrEmpty(itemNumber))
            {
                throw new ArgumentNullException(nameof(itemNumber));
            }
            var quantity = args.Quantity;
            if (quantity <= 0.0m)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity));
            }

            // 새로운 ID 발급.
            var allocationId = NewId.NextGuid();
            
            var response = await _client.GetResponse<InventoryAllocated>(new
            {
                AllocationId = allocationId,
                ItemNumber = itemNumber,
                Quantity = quantity
            });
            _logger.LogInformation("Allocate Inventory Activity  완료합니다. : got InventoryAllocated Message " +
                                   ": {Response}", response);

            // Activity 의 처리 결과를 반환하는 다양한 방법이 Courier.ExecuteContext 에 존재.
            // - context.FaultedWithVariables()
            // - context.Faulted()
            // - context.Terminate()
            // - ...
            return context.Completed<AllocateInventoryLog>(new
            {
                AllocationId = allocationId,
            });
        }

        public async Task<CompensationResult> Compensate(CompensateContext<AllocateInventoryLog> context)
        {
            // 지금까지 해오던 작업을 Rollback 하는 시나리오가 발생.
            _logger.LogWarning("AllocateInventory 작업을 되돌립니다...");
            
            // 현 Activity(=Allocate Inventory, 재고할당)수준에서 Rollback 할 수 있는 처리가 여기 들어감
            // 예를 들면, "재고할당을 취소" 하는 요청을 보내는 작업...
            await context.Publish<AllocationReleaseRequested>(new
            {
                AllocationId = context.Log.AllocationId,
                Reason = "주문 오류 발생"
            });
            
            // 이 Activity 는 Rollback을 위한 작업이 완료되었음을 반환.
            // ("Compenste(보상)" 이라는 의미는 어떤 변경에 대하여 없었던것으로
            //              "보상"하는 방식으로 Masstransit.Courier 가 동작하기 때문)
            return context.Compensated();
        }
    }
}