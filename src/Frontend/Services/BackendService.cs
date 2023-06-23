using Backend.Model;
using System.Net.Mime;
using System.Text;

namespace Frontend.Services
{
    public class BackendServiceClient
    {
        private readonly HttpClient _httpClient;

        public BackendServiceClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<FhirResource>> GetAllFhirResourcesAsync()
        {
            var fhirResources = await _httpClient.GetFromJsonAsync<List<FhirResource>>("/fhir");
            return fhirResources ?? new();
        }

        public async Task<FhirResource?> GetFhirResourceAsync(string? id)
        {
            if (id is null)
                return null;

            var fhirResource = await _httpClient.GetFromJsonAsync<FhirResource>($"/fhir/{id}");
            return fhirResource;
        }

        public async Task<FhirResource?> CreateFhirResourceAsync(FhirResource? fhirResource)
        {
            if (fhirResource?.Json is null)
                return null;

            await _httpClient.PostAsync($"/fhir/{fhirResource.Type}", new StringContent(fhirResource.Json, Encoding.UTF8, MediaTypeNames.Application.Json));

            return fhirResource;
        }

        public async Task<FhirResource?> UpdateFhirResourceAsync(FhirResource? fhirResource)
        {
            if (fhirResource?.Json is null)
                return null;

            await _httpClient.PutAsync($"/fhir/{fhirResource.Type}/{fhirResource.Id}", new StringContent(fhirResource.Json, Encoding.UTF8, MediaTypeNames.Application.Json));

            return fhirResource;
        }

        public async Task DeleteFhirResourceAsync(string? id)
        {
            if (id is null)
                return;

            await _httpClient.DeleteAsync($"/fhir/{id}");
        }
    }
}
