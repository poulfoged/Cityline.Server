using System;
using Cityline.Server;
using Microsoft.AspNetCore.Builder;

// ReSharper disable once CheckNamespace

namespace Microsoft.Extensions.DependencyInjection
{
    public static class CitylinExtensions 
    {
        public static IServiceCollection AddCityline(this IServiceCollection services) 
        {
            return services
                .AddTransient<ICitylineProducer, PingProducer>()
                .AddTransient<ICitylineConsumer, PingConsumer>()
                .AddTransient<CitylineReciever, CitylineReciever>()
                .AddTransient<CitylineServer, CitylineServer>();
        }

        public static IApplicationBuilder UseCityline(this IApplicationBuilder app, Action<CitylineOptions> setupAction = null) 
        {
            CitylineOptions citylineOptions = new();
            setupAction?.Invoke(citylineOptions);

            app.UseWebSockets(new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
            });

            app.UseMiddleware<CitylineMiddleware>(new object[1] { citylineOptions });

            return app;
        }
    }
}