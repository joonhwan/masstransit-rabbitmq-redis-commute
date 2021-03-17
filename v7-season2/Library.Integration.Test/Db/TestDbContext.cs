using System;
using System.Collections.Generic;
using MassTransit.EntityFrameworkCoreIntegration;
using MassTransit.EntityFrameworkCoreIntegration.Mappings;
using Microsoft.EntityFrameworkCore;

namespace Library.Integration.Test.Db
{
    public class TestDbContext : SagaDbContext
    {
        public TestDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override IEnumerable<ISagaClassMap> Configurations => new []
        {
            new ThankYouClassMap(),
        };
    }
}