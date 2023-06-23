using Backend.Database;
using Backend.Model;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry;
using Serilog;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            Serilog.Debugging.SelfLog.Enable(Console.Error);

            // Setup Serilog
            builder.Host.UseSerilog((context, configuration) =>
            {
                configuration.ReadFrom.Configuration(context.Configuration);
            });

            // Configure HTTP logging
            builder.Services.AddHttpLogging(logging =>
            {
                // prevent the following HTTP headers from being [redacted] in the logs
                logging.RequestHeaders.Add("baggage");
                logging.RequestHeaders.Add("traceparent");
            });

            // Add services to the container.
            builder.Services.AddAuthorization();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddDbContext<FhirResourceDb>(opt => opt.UseInMemoryDatabase("FhirResourceList"));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            var app = builder.Build();

            // Enable HTTP logging
            app.UseHttpLogging();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();

            app.MapGet("/fhir", async (FhirResourceDb db, ILogger<Program> logger) =>
            {
                logger.LogInformation("Received request: GET all FHIR resources");

                StartActivity();
                IncrementCounter();

                return await db.FhirResources.ToListAsync();
            });

            app.MapGet("/fhir/{id}", async (string id, FhirResourceDb db, ILogger<Program> logger) =>
            {
                logger.LogInformation("Received request: GET FHIR resource with id {resource.id}", id);
                return await db.FhirResources.FindAsync(id)
                    is FhirResource fhirResource
                        ? Results.Ok(fhirResource)
                        : Results.NotFound();
            });

            app.MapGet("/fhir/{resourceType}/{id}", async (string id, string resourceType, FhirResourceDb db, ILogger<Program> logger) =>
            {
                logger.LogInformation("Received request: GET FHIR resource with id {resource.id} and type {resource.type}", id, resourceType);
                return await db.FhirResources.Where(r => r.Id == id && r.Type == resourceType).SingleOrDefaultAsync()
                    is FhirResource fhirResource
                        ? Results.Ok(fhirResource)
                        : Results.NotFound();
            });

            app.MapPost("/fhir/{resourceType}", async (HttpRequest request, string resourceType, FhirResourceDb db, ILogger<Program> logger) =>
            {
                logger.LogInformation("Received request: POST FHIR resource with type {resource.type}", resourceType);
                var json = await GetRequestBodyAsync(request);
                logger.LogDebug("Request contained JSON {resource.json}", json);

                if (TryParseRequestBody(json, out FhirResource fhirResource))
                {
                    if (!string.Equals(resourceType, fhirResource.Type, StringComparison.OrdinalIgnoreCase))
                        return Results.BadRequest($"Resource type does not match endpoint: {fhirResource.Type} != {resourceType}");

                    db.FhirResources.Add(fhirResource);
                    await db.SaveChangesAsync();

                    return Results.Created($"/fhir/{fhirResource.Id}", fhirResource);
                }
                logger.LogError("Could not parse JSON {json}", json);
                return Results.BadRequest("Could not read request body");
            });

            app.MapPut("/fhir/{resourceType}/{id}", async (string id, string resourceType, HttpRequest request, FhirResourceDb db, ILogger<Program> logger) =>
            {
                logger.LogInformation("Received request: PUT FHIR resource with id {resource.id} and type {resource.type}", id, resourceType);
                var json = await GetRequestBodyAsync(request);
                logger.LogDebug("Request contained JSON {resource.json}", json);

                if (TryParseRequestBody(json, out FhirResource inputFhirResource))
                {
                    if (!string.Equals(id, inputFhirResource.Id, StringComparison.OrdinalIgnoreCase))
                        return Results.BadRequest($"Resource Id {inputFhirResource.Id} does not match id {id} in endpoint URL");

                    if (!string.Equals(resourceType, inputFhirResource.Type, StringComparison.OrdinalIgnoreCase))
                        return Results.BadRequest($"Resource type {inputFhirResource.Type} does not match endpoint {resourceType}");

                    var fhirResource = await db.FhirResources.FindAsync(id);

                    if (fhirResource is null) return Results.NotFound();

                    // Update FHIR resource
                    fhirResource.Type = inputFhirResource.Type;
                    fhirResource.Json = inputFhirResource.Json;

                    await db.SaveChangesAsync();

                    return Results.NoContent();
                }
                logger.LogError("Could not parse JSON {json}", json);
                return Results.BadRequest("Could not read request body");
            });

            app.MapDelete("/fhir/{id}", async (string id, FhirResourceDb db, ILogger<Program> logger) =>
            {
                logger.LogInformation("Received request: DEL FHIR resource with id {resource.id}", id);
                if (await db.FhirResources.FindAsync(id) is FhirResource fhirResource)
                {
                    db.FhirResources.Remove(fhirResource);
                    await db.SaveChangesAsync();
                    return Results.Ok(fhirResource);
                }
                logger.LogWarning("Delete FHIR resource failed, could not find resource with {resource.id}", id);
                return Results.NotFound();
            });

            try
            {
                app.Run();
            }
            finally
            {
                Log.CloseAndFlush();
            }

            async static Task<string> GetRequestBodyAsync(HttpRequest request)
            {
                using StreamReader stream = new(request.Body);
                return await stream.ReadToEndAsync();
            }

            static bool TryParseRequestBody(string json, out FhirResource fhirResource)
            {
                fhirResource = new();

                var fhirJson = JsonSerializer.Deserialize<JsonNode>(json);

                if (fhirJson is null || fhirJson["resourceType"] is null)
                    return false;

                // ensure we have an Id value
                fhirJson["id"] ??= Guid.NewGuid().ToString();

                fhirResource = new()
                {
                    Id = fhirJson["id"]?.GetValue<string>(),
                    Type = fhirJson["resourceType"]?.GetValue<string>(),
                    Json = fhirJson.ToJsonString(),
                };

                return (fhirResource is not null);
            }

            static void StartActivity()
            {
                // Use custom activity
                using var activity = DiagnosticsConfig.ActivitySource.StartActivity("GET all FHIR resources");
                activity?.SetTag("yes", 1);
                activity?.SetTag("no", "Bye, World!");
                activity?.SetTag("frizzle", new int[] { 3, 2, 1 });

                // Read context information from span context
                var spanBaggageItem = Baggage.Current.GetBaggage("ExampleItem");
                activity?.SetTag("ExampleItemReceived", spanBaggageItem);
            }

            static void IncrementCounter()
            {
                // Update custom metric
                DiagnosticsConfig.RequestCounter.Add(1,
                    new("Action", "GET /fhir"),
                    new("Controller", "Backend API"));
            }
        }
    }
}