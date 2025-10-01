using Currency.Api.Errors;
using Currency.Application;
using Currency.Application.Behaviors;
using Currency.Application.Features.Convert;
using Currency.Application.Features.RatesLatest;
using Currency.Infrastructure;
using Currency.Infrastructure.Providers.Frankfurter;
using FluentValidation;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);

// Add Global Error Handling
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Register MediatR and FluentValidation
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(IAssemblyMarker).Assembly)
);
builder.Services.AddValidatorsFromAssembly(typeof(IAssemblyMarker).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

var app = builder.Build();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/openapi/v1.json", "Currency API v1");
        o.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.MapGet(
        "/api/v1/rates/latest",
        async (IMediator mediator, string? @base, CancellationToken ct) =>
        {
            var baseCur = string.IsNullOrWhiteSpace(@base) ? "EUR" : @base!;
            var result = await mediator.Send(new LatestRatesQuery(baseCur), ct);
            return Results.Ok(result);
        }
    )
    .WithName("GetLatestRates")
    .Produces<LatestRatesResponse>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .WithTags("v1");

app.MapPost(
        "/api/v1/convert",
        async (IMediator mediator, ConvertCurrencyCommand body, CancellationToken ct) =>
        {
            var result = await mediator.Send(body, ct);
            return Results.Ok(result);
        }
    )
    .WithName("ConvertCurrency")
    .Produces<ConvertCurrencyResult>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .WithTags("v1");

app.Run();
