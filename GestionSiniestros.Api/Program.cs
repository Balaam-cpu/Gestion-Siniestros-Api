using System.Text.Json.Serialization;
using GestionSiniestros.Api.Data;
using GestionSiniestros.Api.Interfaces;
using GestionSiniestros.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;

const string ApiRoutePrefix = "api/v1";

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("ClaimsDb")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexión 'ConnectionStrings:ClaimsDb'. " +
        "Configúrala con: dotnet user-secrets set ""ConnectionStrings:ClaimsDb"" ""<cadena>""");

builder.Services.AddDbContext<ClaimsDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IClaimService, ClaimService>();

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

const string AngularDevCorsPolicy = "AngularDev";
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularDevCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

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
