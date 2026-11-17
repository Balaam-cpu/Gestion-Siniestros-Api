using System.Diagnostics;
using System.Text.Json.Serialization;
using GestionSiniestros.Api.Data;
using GestionSiniestros.Api.Interfaces;
using GestionSiniestros.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;

const string AngularDevCorsPolicy = "AngularDev";
const string ApiRoutePrefix = "api/v1";

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

        if (context.Exception is not null)
        {
            context.ProblemDetails.Status = StatusCodes.Status500InternalServerError;
            context.ProblemDetails.Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1";
            context.ProblemDetails.Title = "Error interno del servidor.";
            context.ProblemDetails.Detail =
                "Ocurrió un error inesperado. Si el problema persiste, comunícate con soporte e indica el traceId.";
            context.ProblemDetails.Instance = null;
            context.ProblemDetails.Extensions.Clear();
        }

        context.ProblemDetails.Extensions.TryAdd("traceId", traceId);
    };
});

builder.Services
    .AddControllers(options =>
    {
        options.Conventions.Add(new ApiRoutePrefixConvention(ApiRoutePrefix));
    })
    .AddJsonOptions(options =>
    {
        // Los estados viajan como strings ("Open", "InReview", "Closed"), no como números.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
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

app.UseExceptionHandler();

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

file sealed class ApiRoutePrefixConvention(string prefix) : IApplicationModelConvention
{
    private readonly AttributeRouteModel _prefix = new(new RouteAttribute(prefix));

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            foreach (var selector in controller.Selectors)
            {
                selector.AttributeRouteModel = selector.AttributeRouteModel is null
                    ? new AttributeRouteModel(_prefix)
                    : AttributeRouteModel.CombineAttributeRouteModel(_prefix, selector.AttributeRouteModel);
            }
        }
    }
}
