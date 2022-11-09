using Cityline.WebTest.Features.Webpack;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class WebpackExtensions
    {
        public static IServiceCollection AddWebpackFeature(this IServiceCollection services) 
        {
            services
                .AddSingleton<Parser>();
            
            return services;
        }
    }
}