using Currency.Application;
using Currency.Application.Behaviors;
using Currency.Infrastructure;
using Currency.Infrastructure.Providers.Frankfurter;
using FluentValidation;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(IAssemblyMarker).Assembly)
);
builder.Services.AddValidatorsFromAssembly(typeof(IAssemblyMarker).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/openapi/v1.json", "Currency API v1");
        o.RoutePrefix = "swagger";
    });

    app.MapGet(
        "/dev/ping-frankfurter",
        async (FrankfurterClient client, string? @base, HttpContext ctx) =>
        {
            using var resp = await client.GetLatestAsync(@base ?? "EUR", ctx.RequestAborted);
            var body = await resp.Content.ReadAsStringAsync(ctx.RequestAborted);
            return Results.Content(body, "application/json");
        }
    );
}

app.UseHttpsRedirection();

app.Run();
