using Hangfire;
using Hangfire.MemoryStorage;
using HealthChecks.SqlServer;
using MassTransit;
using MassTransitHangfireBug.Consumers;
using Microsoft.Extensions.Configuration;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var services = builder.Services;

var connectionString = builder.Configuration.GetConnectionString("quartz")
            ?? throw new InvalidOperationException("Connection string 'quartz' is not configured.");

services.AddHealthChecks()
    .AddCheck<SqlServerHealthCheck>("sql");

services.AddQuartz(q =>
{
    q.SchedulerName = "MassTransit-Scheduler";
    q.SchedulerId = "AUTO";

    q.UseDefaultThreadPool(tp =>
    {
        tp.MaxConcurrency = 10;
    });

    q.UsePersistentStore(s =>
    {
        s.UseProperties = true;
        s.RetryInterval = TimeSpan.FromSeconds(15);

        s.UsePostgres(connectionString);
        //s.UseSqlServer(connectionString);

        s.UseClustering(c =>
        {
            c.CheckinMisfireThreshold = TimeSpan.FromSeconds(20);
            c.CheckinInterval = TimeSpan.FromSeconds(10);
        });

        var serializerType = builder.Configuration["quartz:serializer:type"]
            ?? throw new InvalidOperationException("Missing Quartz serializer type configuration.");

        s.SetProperty("quartz.serializer.type", serializerType);
    });
});

builder.Services
                .AddMassTransit(x =>
                {
                    x.AddPublishMessageScheduler();

                    x.AddQuartzConsumers();
                    x.AddConsumer<ConvertVideoJobConsumer>();
                    x.AddConsumer<TrackVideoConvertedConsumer>();

                    x.UsingInMemory((context, cfg) =>
                    {
                        cfg.UsePublishMessageScheduler();
                        //cfg.UseHangfireScheduler();

                        cfg.ConfigureEndpoints(context);
                    });


                    x.SetInMemorySagaRepositoryProvider();
                    x.AddJobSagaStateMachines(options => options.FinalizeCompleted = true);



                });

services.Configure<MassTransitHostOptions>(options =>
{
    options.WaitUntilStarted = true;
});

services.AddQuartzHostedService(options =>
{
    options.StartDelay = TimeSpan.FromSeconds(5);
    options.WaitForJobsToComplete = true;
});



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


app.Run();
