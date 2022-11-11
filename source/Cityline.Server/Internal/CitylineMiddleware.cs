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
    internal class CitylineMiddleware
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
            if (context?.WebSockets?.IsWebSocketRequest == false || context.Request.Path != _citylineOptions.Path)
            {
                await _next(context);
                return;
            }

            try
            {

                Console.WriteLine("Request started: " + context.Request.GetEncodedUrl());

                var cancellationToken = context.RequestAborted;

                using var _citylineServer = serviceProvider.GetService<CitylineServer>();
                using var _citylineReciever = serviceProvider.GetService<CitylineReciever>();

                var citylineContext = new Context { RequestUrl = new Uri(context.Request.GetEncodedUrl()), User = context.User };

                // for now, maybe migrate it further per call
                _citylineReciever.SetContext(citylineContext);

                context.RequestAborted.Register(() =>
                {
                    Console.WriteLine("Aborted!" + cancellationToken.IsCancellationRequested);
                });


                using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();

                Console.WriteLine("Here socket is: " + webSocket.State);

                // start reading
                _citylineReciever.StartBackground(webSocket, cancellationToken);

                // Auth
                citylineContext.Headers = await _citylineReciever.WaitFirstFrame<Dictionary<string, string>>("_headers", cancellationToken);

                // State
                citylineContext.Request = await _citylineReciever.WaitFirstFrame<CitylineRequest>("_request", cancellationToken);

                _citylineOptions.Authorization?.Invoke(citylineContext);

                // start writing
                await _citylineServer.WriteStream(webSocket, citylineContext.Request, citylineContext, cancellationToken);
                
            }
            catch (OperationCanceledException ex)
            {
                Console.WriteLine("Operation canceled: " + ex);
                
            }
        }
    }
}