namespace Model
{
    public class FhirResource
    {
        //[Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public DateTime Created { get; set; } = DateTime.Now;

        public string? Type { get; set; }
    }
}
