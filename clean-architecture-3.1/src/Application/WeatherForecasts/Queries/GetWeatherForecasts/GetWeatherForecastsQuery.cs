using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.TodoLists.Commands.UpdateTodoList;
using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.WeatherForecasts.Queries.GetWeatherForecasts
{
    // public class GetWeatherForecastsQuery : IRequest<IEnumerable<WeatherForecast>>
    // {
    // }
    //
    // public class GetWeatherForecastsQueryHandler : IRequestHandler<GetWeatherForecastsQuery, IEnumerable<WeatherForecast>>
    // {
    //     private static readonly string[] Summaries = new[]
    //     {
    //         "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    //     };
    //
    //     public Task<IEnumerable<WeatherForecast>> Handle(GetWeatherForecastsQuery request, CancellationToken cancellationToken)
    //     {
    //         var rng = new Random();
    //
    //         var vm = Enumerable.Range(1, 5).Select(index => new WeatherForecast
    //         {
    //             Date = DateTime.Now.AddDays(index),
    //             TemperatureC = rng.Next(-20, 55),
    //             Summary = Summaries[rng.Next(Summaries.Length)]
    //         });
    //
    //         return Task.FromResult(vm);
    //     }
    // }

    public class WeatherForecastsConsumer : IConsumer<GetWeatherForecasts>
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public async Task Consume(ConsumeContext<GetWeatherForecasts> context)
        {
            var rng = new Random();

            var vm = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            });

            await context.RespondAsync<WeatherForecasts>(new
            {
                Forecasts = vm.ToArray()
            });
        }
    }
    
    public class GetWeatherForecastsValidator : AbstractValidator<GetWeatherForecasts>
    { 
        private readonly IApplicationDbContext _context;

        public GetWeatherForecastsValidator(IApplicationDbContext context)
        {
            _context = context;

            RuleFor(v => v.Page)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Page must be >= 0")
                ;
        }
    }

    public interface GetWeatherForecasts
    {
        int Page { get; }
    }

    public interface WeatherForecasts
    {
        WeatherForecast[] Forecasts { get; }
    }
}
