using System.Diagnostics;
using System.Text.Json.Serialization;
using Asp.Versioning;
using GestionSiniestros.Api.Data;
using GestionSiniestros.Api.Interfaces;
using GestionSiniestros.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

const string AngularDevCorsPolicy = "AngularDev";

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("ClaimsDb")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexión 'ConnectionStrings:ClaimsDb'. ");

builder.Services.AddDbContext<ClaimsDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IClaimService, ClaimService>();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
        context.ProblemDetails.Extensions.TryAdd("traceId", traceId);
    };
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Los estados viajan como strings ("Open", "InReview", "Closed"), no como números.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    });

builder.Services.AddApiVersioning(options =>
{
    options.ReportApiVersions = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
}).AddMvc().AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularDevCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var problem = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            Title = "Error interno del servidor.",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "Ocurrió un error inesperado. Si el problema persiste, comunícate con soporte e indica el traceId."
        };
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", context.RequestAborted);
    });
});

app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors(AngularDevCorsPolicy);
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
