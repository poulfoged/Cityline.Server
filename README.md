![](https://raw.githubusercontent.com/poulfoged/Cityline.Client/master/icon.png) &nbsp; 
[![Build status](https://ci.appveyor.com/api/projects/status/1e3usc72j7547gom?svg=true)](https://ci.appveyor.com/project/poulfoged/cityline-server) &nbsp; 
[![Nuget version](https://img.shields.io/nuget/v/cityline.server)](https://www.nuget.org/packages/Cityline.Server/)

# Cityline.Server

Use one of the clients to connect to this:

- [cityline-client](https://www.npmjs.com/package/cityline-client) (JavaScript client)
- [Cityline.Client](https://www.nuget.org/packages/Cityline.Client/) (dotnet client)  

```
 Client                          This library (dotnet)

 ┌──────────────────────────────┐       ┌──────────────────────────────┐      ┌──────────────────────────────┐
 │                              │       │                              │      │                              ├─┐
 │                              │       │                              │      │                              │ │
 │       Cityline.Client        │◀─────▶│       Cityline.Server        │─────▶│      ICitylineProducer       │ │
 │                              │       │                              │      │                              │ │
 │                              │       │                              │      │                              │ │
 └──────────────────────────────┘       └──────────────────────────────┘      └──┬───────────────────────────┘ │
                                                                                 └─────────────────────────────┘

  - raises events                         - streams data to clients
  - get specific frame                    - calls producers
    (for preloading data)                 - allows state from call to call
  - wait for specific set of frames
    (app initialization)
```

## Demo

See a demo of the server and javascript client [here](https://poulfoged.github.io/Cityline-Chat).

## Getting started

To get started create a producer. A producer is a stateless class (singleton is fine) that produces an output given a condition.
Each producer can store its state in the provided ticket and retrieve it later. The ticket itself is also stored by the clients so even on reconnect we can resume from where we left.

The ticket can also be used to throttle a call by storing a timestamp. 

This is a producer that simply sends a ping (a timestamp in this case) every 5 seconds. So in this case the ticket is only used for throtteling.

Returning null from a producer is a way to say "no news at the moment".

```c#
   public async Task<object> GetFrame(ITicketHolder ticket, IContext context, CancellationToken cancellationToken = default(CancellationToken))
    {
        var myState = ticket.GetTicket<MyState>();

        if (myState != null)
            if ((DateTime.UtcNow - myState.Created).TotalSeconds < 5)
                return null;

        ticket.UpdateTicket(new MyState());

        // simulate some work
        await Task.Delay(2);

        return new { Ping = DateTime.UtcNow };
    }
```

You then add a controller to provide an endpoint for the clients to connect to. The CitylineServer class takes a list of producers. (See linked demo to see how to use DI to manage this list).

```c#
  [HttpPost]  
  public async Task StartStream(CitylineRequest request, CancellationToken cancellationToken = default(CancellationToken))
  {
      var citylineService = new CitylineServer(providers); 
      var context = new Context { RequestUrl = new Uri(Request.GetEncodedUrl()), User = User };
      Response.Headers.Add("content-type", "text/event-stream");
      await citylineService.WriteStream(Response.Body, request, context, cancellationToken);
  } 
```

The context object customized to provide further info (like headers) to the producers. It already contains current user and url.

## Install

Simply add the NuGet package:

`PM> Install-Package Cityline.Server`
