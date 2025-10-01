using Currency.Infrastructure;
using Currency.Infrastructure.Providers.Frankfurter;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/openapi/v1.json", "Currency API v1");
        o.RoutePrefix = "swagger"; // so UI lives at /swagger
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
