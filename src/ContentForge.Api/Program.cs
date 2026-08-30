using System.Threading.RateLimiting;
using ContentForge.Api;
using ContentForge.Api.Infrastructure;
using ContentForge.Application;
using ContentForge.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray(),
                    StringComparer.Ordinal);

            var problem = ProblemDetailsFactory.CreateValidation(context.HttpContext, errors);
            return new UnprocessableEntityObjectResult(problem)
            {
                ContentTypes = { "application/problem+json" },
            };
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = ApiConstants.ApiTitle,
        Version = ApiConstants.Version,
        Description =
            "ContentForge headless CMS REST API. " +
            "Administrative routes require a JWT bearer token. " +
            "Public content routes under /api/v1/public expose published snapshots only — " +
            "drafts, in-review, unpublished, and archived entries are never returned, " +
            "and audit metadata, user identifiers, and persistence identifiers are omitted.",
    });

    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer",
                },
            },
            Array.Empty<string>()
        },
    });

    options.OperationFilter<AllowAnonymousOperationFilter>();
    options.SupportNonNullableReferenceTypes();
});

builder.Services.AddExceptionHandler<ApplicationExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    var existing = options.Events ?? new JwtBearerEvents();
    var previousOnTokenValidated = existing.OnTokenValidated;

    existing.OnChallenge = async context =>
    {
        context.HandleResponse();
        var problem = ProblemDetailsFactory.Create(
            context.HttpContext,
            StatusCodes.Status401Unauthorized,
            "Unauthorized",
            "Authentication is required.",
            ApiConstants.ErrorTypes.Unauthorized);
        await ProblemDetailsFactory.WriteAsync(context.HttpContext, problem).ConfigureAwait(false);
    };

    existing.OnForbidden = async context =>
    {
        var problem = ProblemDetailsFactory.Create(
            context.HttpContext,
            StatusCodes.Status403Forbidden,
            "Forbidden",
            "The caller is not permitted to perform this action.",
            ApiConstants.ErrorTypes.Forbidden);
        await ProblemDetailsFactory.WriteAsync(context.HttpContext, problem).ConfigureAwait(false);
    };

    if (previousOnTokenValidated is not null)
    {
        existing.OnTokenValidated = previousOnTokenValidated;
    }

    options.Events = existing;
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        var problem = ProblemDetailsFactory.Create(
            context.HttpContext,
            StatusCodes.Status429TooManyRequests,
            "Too Many Requests",
            "Public API rate limit exceeded. Retry later.",
            "https://contentforge/errors/rate-limit");
        await ProblemDetailsFactory.WriteAsync(context.HttpContext, problem, cancellationToken).ConfigureAwait(false);
    };

    options.AddPolicy("public", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = httpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsEnvironment("Testing")
                    ? 10_000
                    : 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (corsOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(
            "ContentForgeCors",
            policy => policy
                .WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod());
    });
}

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", $"{ApiConstants.ApiTitle} v{ApiConstants.Version}");
    });
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseHttpsRedirection();

if (corsOrigins.Length > 0)
{
    app.UseCors("ContentForgeCors");
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
