using Asp.Versioning;
using EventBus.Message.Common;
using MassTransit;
using Ordering.API.EventBusConsumer;
using Ordering.API.Extensions;
using Ordering.Application.Extensions;
using Ordering.Infrastructure.Data;
using Ordering.Infrastructure.Extensions;
namespace Ordering.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();

            // Add API Versioning
            builder.Services.AddApiVersioning(options =>
            {
                options.ReportApiVersions = true;
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
            });

            //Application Services
            builder.Services.AddApplicationServices();

            //Infra services
            builder.Services.AddInfraServices(builder.Configuration);

            //Consumer class
            builder.Services.AddScoped<BasketOrderingConsumer>();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Ordering.API", Version = "v1" }); });

            //Mass Transit
            builder.Services.AddMassTransit(config =>
            {
                //Mark this as consumer
                config.AddConsumer<BasketOrderingConsumer>();
                config.UsingRabbitMq((ctx, cfg) =>
                {
                    cfg.Host(builder.Configuration["EventBusSettings:HostAddress"]);
                    //provide the queue name with cosumer settings
                    cfg.ReceiveEndpoint(EventBusConstant.BasketCheckoutQueue, c =>
                    {
                        c.ConfigureConsumer<BasketOrderingConsumer>(ctx);
                    });
                });
            });
            builder.Services.AddMassTransitHostedService();

            var app = builder.Build();

            //Apply db migration
            app.MigrateDatabase<OrderContext>((context, services) =>
            {
                var logger = services.GetService<ILogger<OrderContextSeed>>();
                OrderContextSeed.SeedAsync(context, logger).Wait();
            });

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
