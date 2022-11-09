using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Cityline.Server;
using Cityline.Server.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Cityline.Server
{
    public class CitylineMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly CitylineOptions _citylineOptions;

        public CitylineMiddleware(RequestDelegate next, CitylineOptions citylineOptions)
        {
            _next = next;
            _citylineOptions = citylineOptions;
        }

        public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
        {
            if (context.Request.Path != _citylineOptions.Path)
            {
                await _next(context);
                return;
            }


            using var _citylineServer = serviceProvider.GetService<CitylineServer>();
            var _citylineReciever = serviceProvider.GetService<CitylineReciever>();

            var citylineContext = new Context { RequestUrl = new Uri(context.Request.GetEncodedUrl()), User = context.User };
            var timeoutSource = new CancellationTokenSource(TimeSpan.FromHours(2));
            var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, context.RequestAborted);
            var cancellationToken = linkedSource.Token;

            if (context.WebSockets.IsWebSocketRequest)
            {
                using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();

                cancellationToken = _citylineReciever.StartBackground(webSocket, cancellationToken);

                // Auth
                var headers = await _citylineReciever.WaitFirstFrame<Dictionary<string, string>>("_headers", cancellationToken);


                // State
                var request = await _citylineReciever.WaitFirstFrame<CitylineRequest>("_request", cancellationToken);

                await _citylineServer.WriteStream(webSocket, request, citylineContext, linkedSource.Token);
            }
        }
    }
}