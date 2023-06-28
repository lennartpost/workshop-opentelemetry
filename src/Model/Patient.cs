namespace Model
{
    public class Patient : FhirResource
    {
        public string? Gender { get; set; }

        public string? Name { get; set; }

        public DateTime? Birthdate { get; set; }
    }
}
