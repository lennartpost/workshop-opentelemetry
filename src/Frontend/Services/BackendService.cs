using Model;

namespace Frontend.Services
{
    public class BackendServiceClient
    {
        private readonly HttpClient _httpClient;

        public BackendServiceClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<T>> GetAllFhirResourcesAsync<T>() where T : FhirResource
        {
            var fhirResources = await _httpClient.GetFromJsonAsync<List<T>>("/fhir");
            return fhirResources ?? new();
        }

        public async Task<T?> GetFhirResourceAsync<T>(string? id) where T : FhirResource
        {
            if (id is null)
                return null;

            var fhirResource = await _httpClient.GetFromJsonAsync<T>($"/fhir/{id}");
            return fhirResource;
        }

        public async Task<T?> CreateFhirResourceAsync<T>(T? fhirResource) where T : FhirResource
        {
            if (fhirResource is null)
                return null;

            await _httpClient.PostAsJsonAsync($"/fhir/{fhirResource.Type}", fhirResource);

            return fhirResource;
        }

        public async Task<T?> UpdateFhirResourceAsync<T>(T? fhirResource) where T : FhirResource
        {
            if (fhirResource is null)
                return null;

            await _httpClient.PutAsJsonAsync($"/fhir/{fhirResource.Type}/{fhirResource.Id}", fhirResource);

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
