using Library.Components.StateMachines;
using MassTransit.EntityFrameworkCoreIntegration.Mappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Integration.Test.Db
{
    public class ThankYouClassMap : SagaClassMap<ThankYouSaga>
    {
        protected override void Configure(EntityTypeBuilder<ThankYouSaga> entity, ModelBuilder model)
        {
            // SagaClassMap<T> 은 기본적으로 T 형(NOTE: T 는 ISaga 임)의 CorrelationId 를 Key 로 잡는다. 
            
            // 추가적인 컬럼, 제약사항을 넣는다. 
            entity
                .HasIndex(saga => new
                {
                    saga.BookId,
                    saga.MemberId
                })
                .IsUnique()
                ;

            //entity.ToTable("THANK_YOU","SAGA");
        }
    }
}