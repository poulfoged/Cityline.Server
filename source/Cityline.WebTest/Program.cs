using Cityline.Server;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();


builder.Services
    .AddWebpackFeature()
    .AddCityline();

builder.Services
    .AddHealthChecks()
        .AddCheck<CitylineHealthCheck>(nameof(CitylineHealthCheck));

var app = builder.Build();

app.UseCityline();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

 app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "dist/")),
    RequestPath = "/dist",
});

app.UseEndpoints(endpoints =>
{
    //...
    endpoints.MapHealthChecks("/health-check", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });
    //...
});



app.UseAuthorization();

app.MapRazorPages();




app.Run();

