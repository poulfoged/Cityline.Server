# Cityline

Real-time library for sending events to Cityline clients. Deeply inspired by server sent events but initiated via posts which allow a much bigger state object with state across multiple producers.

[![Build status](https://ci.appveyor.com/api/projects/status/jioe3k751sj14i5i?svg=true)](https://ci.appveyor.com/project/poulfoged/cityline)
[![Nuget version](https://img.shields.io/nuget/v/cityline)](https://www.nuget.org/packages/Cityline/)




```
 Client Side (JS)                        Server side (C#)

 ┌──────────────────────────────┐       ┌──────────────────────────────┐      ┌──────────────────────────────┐
 │                              │       │                              │      │                              ├─┐
 │                              │       │                              │      │                              │ │
 │       cityline-client        │◀─────▶│       CitylineService        │─────▶│      ICitylineProducer       │ │
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

Image by <a href="https://pixabay.com/users/OpenClipart-Vectors-30363/?utm_source=link-attribution&amp;utm_medium=referral&amp;utm_campaign=image&amp;utm_content=148650">OpenClipart-Vectors</a> from <a href="https://pixabay.com/?utm_source=link-attribution&amp;utm_medium=referral&amp;utm_campaign=image&amp;utm_content=148650">Pixabay</a>
