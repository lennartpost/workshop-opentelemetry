using Backend.Model;
using Frontend.Services;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;

namespace Frontend.Controllers
{
    public class PatientController : Controller
    {
        private readonly ILogger<PatientController> _logger;
        private readonly BackendServiceClient _service;

        public PatientController(BackendServiceClient service, ILogger<PatientController> logger)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: PatientController
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Start of GET Patient overview page");

                StartActivity();
                IncrementCounter(nameof(Index));

                var fhirResources = await _service.GetAllFhirResourcesAsync();
                return fhirResources != null ?
                   View(fhirResources) :
                   Problem("FhirResources is null.");
            }
            finally
            {
                _logger.LogInformation("End of GET Patient overview page");
            }
        }

        // GET: PatientController/Details/5
        public async Task<IActionResult> Details(string? id)
        {
            try
            {
                _logger.LogInformation("Start of GET Patient details page {resource.id}", id);
                if (id == null) { return NotFound(); }

                var fhirResource = await _service.GetFhirResourceAsync(id);

                return (fhirResource == null) ? NotFound() : View(fhirResource);
            }
            finally
            {
                _logger.LogInformation("End of GET Patient details page {resource.id}", id);
            }
        }

        // GET: PatientController/Create
        public IActionResult Create()
        {
            try
            {
                _logger.LogInformation("Start of GET Patient Create page");
                return View(new FhirResource());
            }
            finally
            {
                _logger.LogInformation("End of GET Patient Create page");
            }
        }

        // POST: PatientController/Create
        [HttpPost]
        public async Task<IActionResult> Create([Bind("Type,Json")] FhirResource fhirResource)
        {
            try
            {
                _logger.LogInformation("Start of POST Create Patient");
                if (ModelState.IsValid)
                {
                    await _service.CreateFhirResourceAsync(fhirResource);
                    return RedirectToAction(nameof(Index));
                }
                _logger.LogWarning("Submitted FhirResource is not valid. Json: {resource.json}", fhirResource?.Json);
                return View(fhirResource);
            }
            finally
            {
                _logger.LogInformation("End of POST Create Patient");
            }
        }

        // GET: PatientController/Edit/5
        public async Task<IActionResult> Edit(string? id)
        {
            try
            {
                _logger.LogInformation("Start of GET Edit Patient {resource.id} page", id);
                if (id == null) { return NotFound(); }

                var fhirResource = await _service.GetFhirResourceAsync(id);

                if (fhirResource == null) { return NotFound(); }

                return View(fhirResource);
            }
            finally
            {
                _logger.LogInformation(message: "End of GET Edit Patient {resource.id} page", id);
            }
        }

        // POST: PatientController/Edit/5
        [HttpPost]
        public async Task<IActionResult> Edit(string id, [Bind("Id,Type,Json")] FhirResource fhirResource)
        {
            try
            {
                _logger.LogInformation("Start of POST Edit Patient {resource.id}", id);
                if (id != fhirResource.Id) { return NotFound(); }

                if (ModelState.IsValid)
                {
                    try
                    {
                        _logger.LogInformation("Updating {resource.type} {resource.id}. Json: {resource.json}", fhirResource.Type, fhirResource.Id, fhirResource.Json);
                        await _service.UpdateFhirResourceAsync(fhirResource);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in updating patient {resource.id}", id);
                        if (await _service.GetFhirResourceAsync(fhirResource.Id) is null)
                        {
                            _logger.LogError(ex, "Trying to update Patient {resource.id}, but patient was not found", fhirResource.Id);
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }
                return View(fhirResource);
            }
            finally
            {
                _logger.LogInformation("End of POST Edit Patient {resource.id}", id);
            }
        }

        // GET: PatientController/Delete/5
        public async Task<IActionResult> Delete(string? id)
        {
            try
            {
                _logger.LogInformation("Start of GET Delete Patient {resource.id} page", id);
                if (id == null) { return NotFound(); }

                var fhirResource = await _service.GetFhirResourceAsync(id);

                if (fhirResource == null) { return NotFound(); }

                return View(fhirResource);
            }
            finally
            {
                _logger.LogInformation("End of GET Delete Patient {resource.id} page", id);
            }
        }

        // POST: PatientController/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(string? id)
        {
            try
            {
                _logger.LogInformation("Start of POST Delete Patient {resource.id}", id);
                if (id is null) { return NotFound(); }

                await _service.DeleteFhirResourceAsync(id);

                return RedirectToAction(nameof(Index));
            }
            finally
            {
                _logger.LogInformation("End of POST Delete Patient {resource.id}", id);
            }
        }

        static void StartActivity()
        {
            // Track work inside of the request
            using var activity = DiagnosticsConfig.ActivitySource.StartActivity("GET Patient overview");
            activity?.SetTag("foo", 1);
            activity?.SetTag("bar", "Hello, World!");
            activity?.SetTag("shizzle", new int[] { 1, 2, 3 });
            Baggage.SetBaggage("ExampleItem", "The information");
        }

        static void IncrementCounter(string action)
        {
            // Update custom Metric
            DiagnosticsConfig.RequestCounter.Add(1,
                new("Action", action),
                new("Controller", nameof(PatientController)));
        }
    }
}
