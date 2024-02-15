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
            // Upgrade to wss seems to work better if we don't reject non-websockets here
            if (context.Request.Path != _citylineOptions.Path) //context?.WebSockets?.IsWebSocketRequest == false || 
            {
                await _next(context);
                return;
            }

            try
            {

                Console.WriteLine("*** Request started: " + context.Request.GetEncodedUrl() + context?.WebSockets?.IsWebSocketRequest);

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

                // send an empty message, this switches status on the client and we can quickly get to request and headers
                await webSocket.SendAsync(new byte[0], WebSocketMessageType.Text, false, cancellationToken);

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