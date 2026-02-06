using MassTransit;

namespace users_api.Messaging
{
    public static class MassTransitConfig
    {
        public static IServiceCollection AddBus(this IServiceCollection services, IConfiguration cfg)
        {
            services.AddMassTransit(x =>
            {
                x.AddConsumers(typeof(MassTransitConfig).Assembly);
                x.SetKebabCaseEndpointNameFormatter();
                x.UsingRabbitMq((context, busCfg) =>
                {
                    busCfg.Host(cfg["RabbitMQ:Host"], h =>
                    {
                        var user = cfg["RabbitMQ:Username"]; var pass = cfg["RabbitMQ:Password"];
                        if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(pass))
                        {
                            h.Username(user);
                            h.Password(pass);
                        }
                    });
                    busCfg.ConfigureEndpoints(context);
                });
            });
            return services;
        }
    }
}
