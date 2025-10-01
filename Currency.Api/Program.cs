using Currency.Api.Errors;
using Currency.Application;
using Currency.Application.Behaviors;
using Currency.Application.Features.Convert;
using Currency.Application.Features.RatesHistory;
using Currency.Application.Features.RatesLatest;
using Currency.Infrastructure;
using FluentValidation;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);

// Add Global Error Handling
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Add Memory Cache
builder.Services.AddMemoryCache();

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

app.MapGet(
        "/api/v1/rates/history",
        async (
            IMediator mediator,
            string @base,
            DateOnly from,
            DateOnly to,
            int page,
            int pageSize,
            CancellationToken ct
        ) =>
        {
            // defaults if client omits page/pageSize
            var p = page <= 0 ? 1 : page;
            var ps = pageSize <= 0 ? 30 : pageSize;

            var result = await mediator.Send(new HistoryQuery(@base, from, to, p, ps), ct);
            return Results.Ok(result);
        }
    )
    .WithName("GetRatesHistory")
    .Produces<HistoryResponse>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .WithTags("v1");

app.Run();
