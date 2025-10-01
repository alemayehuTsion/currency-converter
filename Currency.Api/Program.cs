using System.Text;
using System.Threading.RateLimiting;
using Currency.Api.Errors;
using Currency.Api.OpenApi;
using Currency.Application;
using Currency.Application.Behaviors;
using Currency.Application.Features.Convert;
using Currency.Application.Features.RatesHistory;
using Currency.Application.Features.RatesLatest;
using Currency.Infrastructure;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog bootstrap (JSON logs)
builder.Host.UseSerilog(
    (ctx, lc) =>
    {
        lc.MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(new Serilog.Formatting.Compact.RenderedCompactJsonFormatter());
    }
);

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});
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

// Authentication (JWT)
builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SigningKey"]!)
            ),
            ClockSkew = TimeSpan.FromSeconds(5),
        };
    });

// Authorization (role-based)
builder.Services.AddAuthorization(opts =>
{
    // read endpoints (latest/history)
    opts.AddPolicy("read", p => p.RequireRole("reader", "converter", "admin"));
    // convert endpoint
    opts.AddPolicy("convert", p => p.RequireRole("converter", "admin"));
    // admin-only (future: health/admin)
    opts.AddPolicy("admin", p => p.RequireRole("admin"));
});

builder.Services.AddRateLimiter(opts =>
{
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // partition key from JWT sub/client or remote IP
    static string Key(HttpContext ctx)
    {
        var user = ctx.User;
        var sub = user?.FindFirst("client_id")?.Value ?? user?.FindFirst("sub")?.Value;
        return !string.IsNullOrWhiteSpace(sub)
            ? $"user:{sub}"
            : $"ip:{ctx.Connection.RemoteIpAddress}";
    }

    // 60 req/min for read endpoints
    opts.AddPolicy(
        "read",
        ctx =>
            RateLimitPartition.GetFixedWindowLimiter(
                Key(ctx),
                _ => new FixedWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(1),
                    PermitLimit = 60,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                }
            )
    );

    // 30 req/min for convert endpoint
    opts.AddPolicy(
        "convert",
        ctx =>
            RateLimitPartition.GetFixedWindowLimiter(
                Key(ctx),
                _ => new FixedWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(1),
                    PermitLimit = 30,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                }
            )
    );
});

builder
    .Services.AddOpenTelemetry()
    .WithTracing(tracer =>
    {
        tracer
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault().AddService(serviceName: "Currency.Api")
            )
            .AddAspNetCoreInstrumentation(o =>
            {
                o.RecordException = true;
            })
            .AddHttpClientInstrumentation(o =>
            {
                o.RecordException = true;
                o.FilterHttpRequestMessage = msg =>
                {
                    // could be filtered non-Frankfurter hosts later
                    return true;
                };
            })
            // dev exporter (pretty-prints spans to console)
            .AddConsoleExporter();

        // Should be changed for real backends later
    });

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging(opts =>
{
    // add useful fields to each request log
    opts.EnrichDiagnosticContext = (diag, http) =>
    {
        var act = System.Diagnostics.Activity.Current;
        diag.Set("TraceId", act?.TraceId.ToString() ?? "");
        diag.Set("SpanId", act?.SpanId.ToString() ?? "");
        diag.Set("ClientIP", http.Connection.RemoteIpAddress?.ToString() ?? "");
        diag.Set("UserId", http.User?.FindFirst("sub")?.Value ?? "");
        diag.Set("ClientId", http.User?.FindFirst("client_id")?.Value ?? "");
    };
});
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/openapi/v1.json", "Currency API v1");
        o.RoutePrefix = "swagger";
    });

    app.MapGet(
            "/dev/token",
            (string? roles, IConfiguration cfg) =>
            {
                var roleList = (roles ?? "reader").Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                );

                var claims = new List<System.Security.Claims.Claim>
                {
                    new("sub", "dev-user-1"),
                    new("client_id", "dev-client-1"),
                };
                claims.AddRange(
                    roleList.Select(r => new System.Security.Claims.Claim(
                        System.Security.Claims.ClaimTypes.Role,
                        r
                    ))
                );

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:SigningKey"]!));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                    issuer: cfg["Jwt:Issuer"],
                    audience: cfg["Jwt:Audience"],
                    claims: claims,
                    notBefore: DateTime.UtcNow.AddMinutes(-1),
                    expires: DateTime.UtcNow.AddHours(1),
                    signingCredentials: creds
                );

                var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(
                    token
                );
                return Results.Ok(new { token = jwt, roles = roleList });
            }
        )
        .WithDescription(
            "Dev-only: issues a JWT with the requested roles, e.g., ?roles=reader,converter"
        );
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
    .RequireRateLimiting("read")
    .RequireAuthorization("read")
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
    .RequireRateLimiting("convert")
    .RequireAuthorization("convert")
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
    .RequireRateLimiting("read")
    .RequireAuthorization("read")
    .Produces<HistoryResponse>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .WithTags("v1");

app.Run();

public partial class Program { }
