using Hangfire;
using Hangfire.MemoryStorage;
using MassTransit;
using MassTransitHangfireBug.Consumers;

namespace MassTransitHangfireBug.MassStartup
{
    public static class MassHangFire    {
        public static WebApplicationBuilder ConfigureHangFire(this WebApplicationBuilder builder)
        {

            var services = builder.Services;
            services.AddHangfireServer();

            // Add Hangfire with PostgreSQL storage
            services.AddHangfire(config =>
            {
                config.UseRecommendedSerializerSettings();
                config.UseMemoryStorage();
            });

            builder.Services
                            .AddMassTransit(x =>
                            {
                                x.AddPublishMessageScheduler();

                                x.AddHangfireConsumers();
                                x.AddConsumer<ConvertVideoJobConsumer>();

                                x.UsingInMemory((context, cfg) =>
                                {
                                    cfg.UsePublishMessageScheduler();
                                    //cfg.UseHangfireScheduler();

                                    cfg.ConfigureEndpoints(context);
                                });


                                x.SetInMemorySagaRepositoryProvider();
                                x.AddJobSagaStateMachines(options => options.FinalizeCompleted = true);



                            });

            return builder;
        }
    }
}
