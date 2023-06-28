using Backend.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Model;
using OpenTelemetry;
using Serilog;

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

                return await db.PatientResources.ToListAsync();
            });

            app.MapGet("/fhir/{id}", async (string id, FhirResourceDb db, ILogger<Program> logger) =>
            {
                logger.LogInformation("Received request: GET FHIR resource with id {resource.id}", id);
                return await db.PatientResources.FindAsync(id)
                    is Patient fhirResource
                        ? Results.Ok(fhirResource)
                        : Results.NotFound();
            });

            app.MapGet("/fhir/{resourceType}/{id}", async (string id, string resourceType, FhirResourceDb db, ILogger<Program> logger) =>
            {
                logger.LogInformation("Received request: GET FHIR resource with id {resource.id} and type {resource.type}", id, resourceType);
                return await db.PatientResources.Where(r => r.Id == id && r.Type == resourceType).SingleOrDefaultAsync()
                    is Patient fhirResource
                        ? Results.Ok(fhirResource)
                        : Results.NotFound();
            });

            app.MapPost("/fhir/{resourceType}", async (string resourceType, [FromBody] Patient patient, FhirResourceDb db, ILogger<Program> logger) =>
            {
                logger.LogInformation("Received request: POST FHIR resource with type {resource.type}", resourceType);

                if (!string.Equals(resourceType, patient.Type, StringComparison.OrdinalIgnoreCase))
                    return Results.BadRequest($"Resource type does not match endpoint: {patient.Type} != {resourceType}");

                db.PatientResources.Add(patient);
                await db.SaveChangesAsync();

                return Results.Created($"/fhir/{patient.Id}", patient);
            });

            app.MapPut("/fhir/{resourceType}/{id}", async (string id, string resourceType, [FromBody] Patient patient, FhirResourceDb db, ILogger<Program> logger) =>
            {
                logger.LogInformation("Received request: PUT FHIR resource with id {resource.id} and type {resource.type}", id, resourceType);

                if (!string.Equals(id, patient.Id, StringComparison.OrdinalIgnoreCase))
                    return Results.BadRequest($"Resource Id {patient.Id} does not match id {id} in endpoint URL");

                if (!string.Equals(resourceType, patient.Type, StringComparison.OrdinalIgnoreCase))
                    return Results.BadRequest($"Resource type {patient.Type} does not match endpoint {resourceType}");

                var foundPatient = await db.PatientResources.FindAsync(id);

                if (foundPatient is null) return Results.NotFound();

                // Update FHIR patient
                foundPatient.Type = patient.Type;
                foundPatient.Gender = patient.Gender;
                foundPatient.Name = patient.Name;
                foundPatient.Birthdate = patient.Birthdate;

                await db.SaveChangesAsync();

                return Results.NoContent();
            });

            app.MapDelete("/fhir/{id}", async (string id, FhirResourceDb db, ILogger<Program> logger) =>
            {
                logger.LogInformation("Received request: DEL FHIR resource with id {resource.id}", id);
                if (await db.PatientResources.FindAsync(id) is Patient patient)
                {
                    db.PatientResources.Remove(patient);
                    await db.SaveChangesAsync();
                    return Results.Ok(patient);
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